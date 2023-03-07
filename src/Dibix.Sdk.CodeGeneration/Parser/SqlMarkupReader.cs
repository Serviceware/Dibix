using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public static class SqlMarkupReader
    {
        private static readonly ConcurrentDictionary<SqlMarkupCacheKey, ISqlMarkupDeclaration> HeaderCache = new ConcurrentDictionary<SqlMarkupCacheKey, ISqlMarkupDeclaration>();

        public static ISqlMarkupDeclaration Read(TSqlFragment fragment, SqlMarkupCommentKind commentKind, string source, ILogger logger) => HeaderCache.GetOrAdd(new SqlMarkupCacheKey(commentKind, fragment), x => ReadCore(x.Fragment, x.CommentKind, source, logger));
        
        private static ISqlMarkupDeclaration ReadCore(TSqlFragment fragment, SqlMarkupCommentKind commentKind, string source, ILogger logger)
        {
            TSqlTokenType expectedCommentTokenType = ToTokenType(commentKind);

            Stack<TSqlParserToken> tokens = new Stack<TSqlParserToken>();
            for (int i = fragment.FirstTokenIndex - 1; i >= 0; i--)
            {
                TSqlParserToken token = fragment.ScriptTokenStream[i];
                if (token.TokenType == TSqlTokenType.WhiteSpace)
                    continue;

                if (token.TokenType is not TSqlTokenType.SingleLineComment and not TSqlTokenType.MultilineComment) 
                    break;

                if (token.TokenType == expectedCommentTokenType)
                    tokens.Push(token);
            }

            SqlMarkupDeclaration map = new SqlMarkupDeclaration();
            foreach (TSqlParserToken token in tokens)
            {
                Read(map, token, source, logger);
            }

            return map;
        }

        private static void Read(SqlMarkupDeclaration target, TSqlParserToken token, string source, ILogger logger)
        {
            string text = token.Text;
            switch (token.TokenType)
            {
                case TSqlTokenType.SingleLineComment:
                    text = SanitizeSingleLineComment(text);
                    break;

                case TSqlTokenType.MultilineComment:
                    text = SanitizeMultiLineComment(text);
                    break;

                default:
                    return;
            }

            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0) 
                    continue;

                Read(target, token.Line + i, token.Column, line, source, logger);
            }
        }

        private static void Read(SqlMarkupDeclaration target, int line, int column, string text, string source, ILogger logger)
        {
            StringBuilder elementName = new StringBuilder();
            StringBuilder propertyLeft = new StringBuilder();
            StringBuilder propertyRight = new StringBuilder();

            TokenType previousToken = default;
            ReaderState currentState = ReaderState.Start;
            SqlElement element = null;
            int currentPropertyColumn = column;
            int currentPropertyValueColumn = column;

            for (int i = 0; i < text.Length; i++)
            {
                char @char = text[i];

                TokenType currentToken;
                switch (@char)
                {
                    case ' ':
                        currentToken = TokenType.Delimiter;
                        break;

                    case '@':
                        currentToken = TokenType.Element;
                        break;

                    case ':':
                        currentToken = TokenType.Property;
                        break;

                    default:
                        currentToken = TokenType.Default;
                        break;
                }

                // Ignore multiple delimiters and do not modify state constantly
                if (currentToken == TokenType.Delimiter && previousToken == TokenType.Delimiter)
                    continue;

                switch (currentToken)
                {
                    // X
                    case TokenType.Default when currentState == ReaderState.Start:
                        return;

                    // @X
                    case TokenType.Default when currentState == ReaderState.ElementName:
                        elementName.Append(@char);
                        break;

                    // @a X
                    case TokenType.Default when currentState == ReaderState.Value:
                        if (previousToken == TokenType.Delimiter)
                        {
                            currentPropertyColumn = column + i;
                            currentPropertyValueColumn = currentPropertyColumn;
                        }

                        propertyLeft.Append(@char);
                        break;

                    // @a X
                    // or
                    // @a a:X
                    case TokenType.Default when currentState == ReaderState.Property:
                        propertyRight.Append(@char);
                        break;

                    
                    //  
                    case TokenType.Delimiter when currentState == ReaderState.Start:
                        break;

                    // @X 
                    case TokenType.Delimiter when currentState == ReaderState.ElementName:
                        currentState = ReaderState.Value;
                        break;

                    // @a X 
                    case TokenType.Delimiter when currentState == ReaderState.Value:
                        CollectValueOrProperty(element, currentState, currentPropertyColumn, currentPropertyValueColumn, logger, ref propertyLeft, ref propertyRight);
                        break;

                    // @a a:X 
                    case TokenType.Delimiter when currentState == ReaderState.Property:
                        CollectValueOrProperty(element, currentState, currentPropertyColumn, currentPropertyValueColumn, logger, ref propertyLeft, ref propertyRight);
                        currentState = ReaderState.Value;
                        break;


                    // @
                    case TokenType.Element when currentState == ReaderState.Start:
                        element = new SqlElement(source, line, column + i);
                        currentState = ReaderState.ElementName;
                        break;

                    // @@
                    case TokenType.Element when currentState == ReaderState.ElementName:
                        return;

                    // @a X@
                    case TokenType.Element when currentState == ReaderState.Value:
                        return;

                    // @a a:@
                    case TokenType.Element when currentState == ReaderState.Property:
                        return;


                    // :
                    case TokenType.Property when currentState == ReaderState.Start:
                        return;

                    // @:
                    case TokenType.Property when currentState == ReaderState.ElementName:
                        return;

                    // @a x:
                    case TokenType.Property when currentState == ReaderState.Value:
                        currentPropertyValueColumn = column + i + 1;
                        currentState = ReaderState.Property;
                        break;

                    // @a x::
                    case TokenType.Property when currentState == ReaderState.Property:
                        return;
                }

                previousToken = currentToken;
            }

            if (element == null)
                return;

            element.Name = elementName.ToString().Trim();

            // Only support PascalCase
            if (!Char.IsUpper(element.Name[0]))
                return;

            if (!SqlMarkupKey.IsDefined(element.Name))
                logger.LogError($"Unexpected markup element '{element.Name}'", element.Location.Source, element.Location.Line, element.Location.Column);

            // Finish collecting value/property before EOL
            CollectValueOrProperty(element, currentState, currentPropertyColumn, currentPropertyValueColumn, logger, ref propertyLeft, ref propertyRight);

            target.Add(element);
        }

        private static void CollectValueOrProperty(SqlElement element, ReaderState currentState, int propertyColumn, int valueColumn, ILogger logger, ref StringBuilder propertyLeft, ref StringBuilder propertyRight)
        {
            if (propertyLeft.Length == 0 && propertyRight.Length == 0)
                return;

            string propertyLeftValue = propertyLeft.ToString().Trim();
            switch (currentState)
            {
                case ReaderState.Value:
                    element.SetValue(propertyLeftValue, propertyColumn, valueColumn, logger);
                    break;

                case ReaderState.Property:
                    string propertyRightValue = propertyRight.ToString().Trim();
                    element.AddProperty(propertyLeftValue, propertyRightValue, propertyColumn, valueColumn, logger);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
            }

            propertyLeft = new StringBuilder();
            propertyRight = new StringBuilder();
        }

        private static string SanitizeSingleLineComment(string text)
        {
            StringBuilder sb = new StringBuilder(text);

            for (int i = 0; i < sb.Length; i++)
            {
                char @char = sb[i];
                if (@char == '-' && i + 1 < sb.Length && sb[i + 1] == '-')
                {
                    sb.Replace('-', ' ', i, 2);
                    break;
                }
            }

            string sanitized = sb.ToString();
            return sanitized;
        }

        private static string SanitizeMultiLineComment(string text)
        {
            StringBuilder sb = new StringBuilder(text);

            for (int i = 0; i < sb.Length; i++)
            {
                char @char = sb[i];
                if (@char == '/' && i + 1 < sb.Length && sb[i + 1] == '*')
                {
                    sb.Replace('/', ' ', i, 1);
                    sb.Replace('*', ' ', i + 1, 1);
                    break;
                }
            }

            for (int i = sb.Length - 1; i >= 0; i--)
            {
                char @char = sb[i];
                if (@char == '/' && i - 1 >= 0 && sb[i - 1] == '*')
                {
                    sb.Replace('/', ' ', i, 1);
                    sb.Replace('*', ' ', i - 1, 1);
                    break;
                }
            }

            string sanitized = sb.ToString();
            return sanitized;
        }

        private static TSqlTokenType ToTokenType(SqlMarkupCommentKind commentKind)
        {
            switch (commentKind)
            {
                case SqlMarkupCommentKind.SingleLine: return TSqlTokenType.SingleLineComment;
                case SqlMarkupCommentKind.MultiLine: return TSqlTokenType.MultilineComment;
                default: throw new ArgumentOutOfRangeException(nameof(commentKind), commentKind, null);
            }
        }

        private readonly struct SqlMarkupCacheKey
        {
            public SqlMarkupCommentKind CommentKind { get; }
            public TSqlFragment Fragment { get; }

            public SqlMarkupCacheKey(SqlMarkupCommentKind commentKind, TSqlFragment fragment)
            {
                CommentKind = commentKind;
                Fragment = fragment;
            }
        }

        private enum TokenType
        {
            Default,
            Delimiter,
            Element,
            Property
        }

        private enum ReaderState
        {
            Start,
            ElementName,
            Value,
            Property
        }

        private sealed class SqlElement : ISqlElement
        {
            private readonly IDictionary<string, SqlMarkupProperty> _properties;

            public string Name { get; set; }
            public Token<string> Value { get; private set; }
            public SourceLocation Location { get; }
            public IEnumerable<ISqlElementProperty> Properties => _properties.Values;

            public SqlElement(string source, int line, int column)
            {
                Location = new SourceLocation(source, line, column);
                _properties = new Dictionary<string, SqlMarkupProperty>();
            }

            public bool TryGetPropertyValue(string propertyName, bool isDefault, out Token<string> value)
            {
                if (_properties.TryGetValue(propertyName, out SqlMarkupProperty property))
                {
                    value = property.Value;
                    return true;
                }

                if (isDefault && Value != null)
                {
                    value = Value;
                    return true;
                }

                value = null;
                return false;
            }

            public Token<string> GetPropertyValue(string name) => _properties.TryGetValue(name, out SqlMarkupProperty property) ? property.Value : null;

            public void SetValue(string value, int propertyColumn, int valueColumn, ILogger logger)
            {
                if (Value != null)
                {
                    logger.LogError($"Multiple default properties specified for @{Name}", Location.Source, Location.Line, propertyColumn);
                    return;
                }
                Value = new Token<string>(value, new SourceLocation(Location.Source, Location.Line, valueColumn));
            }

            public void AddProperty(string name, string value, int propertyColumn, int valueColumn, ILogger logger)
            {
                if (_properties.ContainsKey(name))
                {
                    logger.LogError($"Duplicate property for @{Name}.{name}", Location.Source, Location.Line, propertyColumn);
                    return;
                }

                if (value.Length == 0)
                {
                    logger.LogError($"Missing value for '{name}' property", Location.Source, Location.Line, propertyColumn);
                    return;
                }

                Token<string> propertyName = new Token<string>(name, new SourceLocation(Location.Source, Location.Line, propertyColumn));
                Token<string> propertyValue = new Token<string>(value, new SourceLocation(Location.Source, Location.Line, valueColumn));
                _properties.Add(name, new SqlMarkupProperty(propertyName, propertyValue));
            }
        }

        private sealed class SqlMarkupProperty : ISqlElementProperty
        {
            public Token<string> Name { get; }
            public Token<string> Value { get; }

            public SqlMarkupProperty(Token<string> name, Token<string> value)
            {
                Name = name;
                Value = value;
            }
        }

        private sealed class SqlMarkupDeclaration : ISqlMarkupDeclaration
        {
            private readonly IDictionary<string, IList<SqlElement>> _elements = new Dictionary<string, IList<SqlElement>>();

            public bool HasElements => _elements.Any();
            public ICollection<string> ElementNames => _elements.Keys;

            public bool TryGetSingleElement(string name, string source, ILogger logger, out ISqlElement element)
            {
                if (!TryGetSingleElement(name, source, logger, out SqlElement internalElement))
                {
                    element = null;
                    return false;
                }

                element = internalElement;
                return true;
            }

            public bool TryGetSingleElementValue(string name, string source, ILogger logger, out string value)
            {
                if (!TryGetSingleElementValue(name, source, logger, out Token<string> elementValue))
                {
                    value = null;
                    return false;
                }

                value = elementValue.Value;
                return true;
            }
            public bool TryGetSingleElementValue(string name, string source, ILogger logger, out Token<string> value)
            {
                if (!TryGetSingleElement(name, source, logger, out SqlElement element))
                {
                    value = null;
                    return false;
                }

                if (element.Value == null)
                {
                    logger.LogError($"Element has no value: {name}", source, element.Location.Line, element.Location.Column);
                    value = null;
                    return false;
                }

                value = element.Value;
                return true;
            }

            public bool HasSingleElement(string name, string source, ILogger logger) => TryGetSingleElement(name, source, logger, out ISqlElement _);

            public IEnumerable<ISqlElement> GetElements(string name) => _elements.TryGetValue(name, out IList<SqlElement> elements) ? elements : Enumerable.Empty<ISqlElement>();
            
            public void Add(SqlElement element)
            {
                if (!_elements.TryGetValue(element.Name, out IList<SqlElement> elements))
                {
                    elements = new Collection<SqlElement>();
                    _elements.Add(element.Name, elements);
                }
                elements.Add(element);
            }

            private bool TryGetSingleElement(string name, string source, ILogger logger, out SqlElement element)
            {
                if (!_elements.TryGetValue(name, out IList<SqlElement> elements))
                {
                    element = null;
                    return false;
                }

                if (!elements.Any())
                    throw new InvalidOperationException($"Got element registration with no elements: {name}");

                if (elements.Count > 1)
                {
                    logger.LogError($"Found multiple elements with name: {name}", source, elements[1].Location.Line, elements[1].Location.Column);
                    element = null;
                    return false;
                }

                element = elements[0];
                return true;
            }
        }
    }
}