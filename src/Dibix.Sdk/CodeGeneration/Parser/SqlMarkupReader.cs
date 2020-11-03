using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public static class SqlMarkupReader
    {
        public static ISqlMarkupDeclaration ReadHeader(TSqlFragment fragment, string source, ILogger logger)
        {
            SqlMarkupDeclaration map = new SqlMarkupDeclaration();

            foreach (TSqlParserToken token in fragment.AsEnumerable())
            {
                if (token.TokenType == TSqlTokenType.WhiteSpace)
                    continue;

                // Since we are reading the header, stop processing, once any non-comment T-SQL token occurs
                bool isComment = token.TokenType == TSqlTokenType.MultilineComment || TSqlTokenType.SingleLineComment == token.TokenType;
                if (!isComment)
                    break;

                Read(map, token, source, logger);
            }

            return map;
        }

        public static ISqlMarkupDeclaration ReadFragment(TSqlFragment fragment, string source, ILogger logger)
        {
            int startIndex = fragment.FirstTokenIndex;
            TSqlParserToken token;
            do
            {
                token = fragment.ScriptTokenStream[--startIndex];
            } while (token.TokenType == TSqlTokenType.WhiteSpace);

            SqlMarkupDeclaration map = new SqlMarkupDeclaration();
            Read(map, token, source, logger);
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
                        CollectValueOrProperty(element, currentState, currentPropertyColumn, currentPropertyValueColumn, source, logger, ref propertyLeft, ref propertyRight);
                        break;

                    // @a a:X 
                    case TokenType.Delimiter when currentState == ReaderState.Property:
                        CollectValueOrProperty(element, currentState, currentPropertyColumn, currentPropertyValueColumn, source, logger, ref propertyLeft, ref propertyRight);
                        currentState = ReaderState.Value;
                        break;


                    // @
                    case TokenType.Element when currentState == ReaderState.Start:
                        element = new SqlElement(line, column + i);
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

            // Finish collecting value/property before EOL
            CollectValueOrProperty(element, currentState, currentPropertyColumn, currentPropertyValueColumn, source, logger, ref propertyLeft, ref propertyRight);

            target.Add(element);
        }

        private static void CollectValueOrProperty(SqlElement element, ReaderState currentState, int propertyColumn, int valueColumn, string source, ILogger logger, ref StringBuilder propertyLeft, ref StringBuilder propertyRight)
        {
            if (propertyLeft.Length == 0 && propertyRight.Length == 0)
                return;

            string propertyLeftValue = propertyLeft.ToString().Trim();
            switch (currentState)
            {
                case ReaderState.Value:
                    element.SetValue(propertyLeftValue, propertyColumn, valueColumn, source, logger);
                    break;

                case ReaderState.Property:
                    string propertyRightValue = propertyRight.ToString().Trim();
                    element.AddProperty(propertyLeftValue, propertyRightValue, propertyColumn, valueColumn, source, logger);
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
            public ISqlElementValue Value { get; private set; }
            public int Line { get; }
            public int Column { get; }

            public SqlElement(int line, int column)
            {
                this._properties = new Dictionary<string, SqlMarkupProperty>();
                this.Line = line;
                this.Column = column;
            }

            public bool TryGetPropertyValue(string propertyName, bool isDefault, out ISqlElementValue value)
            {
                if (this._properties.TryGetValue(propertyName, out SqlMarkupProperty property))
                {
                    value = property;
                    return true;
                }

                if (isDefault && this.Value != null)
                {
                    value = this.Value;
                    return true;
                }

                value = null;
                return false;
            }

            public ISqlElementValue GetPropertyValue(string name) => this._properties.TryGetValue(name, out SqlMarkupProperty property) ? property : null;

            public void SetValue(string value, int propertyColumn, int valueColumn, string source, ILogger logger)
            {
                if (this.Value != null)
                {
                    logger.LogError(null, $"Multiple default properties specified for @{this.Name}", source, this.Line, propertyColumn);
                    return;
                }
                this.Value = new SqlElementValue(value, this.Line, valueColumn);
            }

            public void AddProperty(string name, string value, int propertyColumn, int valueColumn, string source, ILogger logger)
            {
                if (this._properties.ContainsKey(name))
                {
                    logger.LogError(null, $"Duplicate property for @{this.Name}.{name}", source, this.Line, propertyColumn);
                    return;
                }

                if (value.Length == 0)
                {
                    logger.LogError(null, $"Missing value for '{name}' property", source, this.Line, propertyColumn);
                    return;
                }

                this._properties.Add(name, new SqlMarkupProperty(name, value, this.Line, valueColumn));
            }
        }

        private sealed class SqlElementValue : ISqlElementValue
        {
            public string Value { get; }
            public int Line { get; }
            public int Column { get; }

            public SqlElementValue(string value, int line, int column)
            {
                this.Value = value;
                this.Line = line;
                this.Column = column;
            }
        }

        private sealed class SqlMarkupProperty : ISqlElementValue
        {
            public string Name { get; }
            public string Value { get; }
            public int Line { get; }
            public int Column { get; }

            public SqlMarkupProperty(string name, string value, int line, int column)
            {
                this.Name = name;
                this.Value = value;
                this.Line = line;
                this.Column = column;
            }
        }

        private sealed class SqlMarkupDeclaration : ISqlMarkupDeclaration
        {
            private readonly IDictionary<string, IList<SqlElement>> _elements = new Dictionary<string, IList<SqlElement>>();

            public bool HasElements => this._elements.Any();

            public bool TryGetSingleElement(string name, string source, ILogger logger, out ISqlElement element)
            {
                if (!this.TryGetSingleElement(name, source, logger, out SqlElement internalElement))
                {
                    element = null;
                    return false;
                }

                element = internalElement;
                return true;
            }

            public bool TryGetSingleElementValue(string name, string source, ILogger logger, out string value)
            {
                if (!this.TryGetSingleElementValue(name, source, logger, out ISqlElementValue elementValue))
                {
                    value = null;
                    return false;
                }

                value = elementValue.Value;
                return true;
            }
            public bool TryGetSingleElementValue(string name, string source, ILogger logger, out ISqlElementValue value)
            {
                if (!this.TryGetSingleElement(name, source, logger, out SqlElement element))
                {
                    value = null;
                    return false;
                }

                if (element.Value == null)
                {
                    logger.LogError(null, $"Element has no value: {name}", source, element.Line, element.Column);
                    value = null;
                    return false;
                }

                value = element.Value;
                return true;
            }

            public bool HasSingleElement(string name, string source, ILogger logger) => this.TryGetSingleElement(name, source, logger, out ISqlElement _);

            public IEnumerable<ISqlElement> GetElements(string name) => this._elements.TryGetValue(name, out IList<SqlElement> elements) ? elements : Enumerable.Empty<ISqlElement>();
            
            public void Add(SqlElement element)
            {
                if (!this._elements.TryGetValue(element.Name, out IList<SqlElement> elements))
                {
                    elements = new Collection<SqlElement>();
                    this._elements.Add(element.Name, elements);
                }
                elements.Add(element);
            }

            private bool TryGetSingleElement(string name, string source, ILogger logger, out SqlElement element)
            {
                if (!this._elements.TryGetValue(name, out IList<SqlElement> elements))
                {
                    element = null;
                    return false;
                }

                if (!elements.Any())
                    throw new InvalidOperationException($"Got element registration with no elements: {name}");

                if (elements.Count > 1)
                {
                    logger.LogError(null, $"Found multiple elements with name: {name}", source, elements[1].Line, elements[1].Column);
                    element = null;
                    return false;
                }

                element = elements[0];
                return true;
            }
        }
    }
}