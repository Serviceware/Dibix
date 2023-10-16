using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CodeGenerationContext
    {
        private static readonly IDictionary<PrimitiveType, string> PrimitiveTypeMap = new Dictionary<PrimitiveType, string>
        {
            [PrimitiveType.Boolean]        = "bool"
          , [PrimitiveType.Byte]           = "byte"
          , [PrimitiveType.Int16]          = "short"
          , [PrimitiveType.Int32]          = "int"
          , [PrimitiveType.Int64]          = "long"
          , [PrimitiveType.Float]          = "float"
          , [PrimitiveType.Double]         = "double"
          , [PrimitiveType.Decimal]        = "decimal"
          , [PrimitiveType.Binary]         = "byte[]"
          , [PrimitiveType.Stream]         = "System.IO.Stream"
          , [PrimitiveType.DateTime]       = "System.DateTime"
          , [PrimitiveType.DateTimeOffset] = "System.DateTimeOffset"
          , [PrimitiveType.String]         = "string"
          , [PrimitiveType.Uri]            = "System.Uri"
          , [PrimitiveType.UUID]           = "System.Guid"
          , [PrimitiveType.Xml]            = "System.Xml.Linq.XElement"
        };
        private static readonly PrimitiveType[] ReferenceTypes =
        {
            PrimitiveType.Binary
          , PrimitiveType.String
          , PrimitiveType.Xml
        };
        private readonly CSharpRoot _root;
        private readonly ILogger _logger;
        private readonly string _rootNamespace;
        private string _currentNamespace;
        
        public CodeGenerationModel Model { get; }
        public ISchemaRegistry SchemaRegistry { get; }
        public bool WriteGuardChecks { get; set; }

        internal CodeGenerationContext(CSharpRoot root, CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            _root = root;
            _rootNamespace = model.RootNamespace;
            _currentNamespace = _rootNamespace;
            Model = model;
            SchemaRegistry = schemaRegistry;
            _logger = logger;
        }

        public CodeGenerationContext AddUsing(string @using)
        {
            _root.AddUsing(@using);
            return this;
        }

        public CodeGenerationContext AddSeparator()
        {
            _root.Output.AddSeparator();
            return this;
        }

        public CSharpStatementScope CreateOutputScope() => CreateOutputScope(_currentNamespace);
        public CSharpStatementScope CreateOutputScope(string @namespace) => _root.Output.BeginScope(@namespace);

        public SchemaDefinition GetSchema(SchemaTypeReference reference) => SchemaRegistry.GetSchema(reference);

        public string ResolveTypeName(TypeReference reference, EnumerableBehavior enumerableBehavior = EnumerableBehavior.Enumerable)
        {
            string typeName;
            bool requiresNullabilityMarker;
            switch (reference)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                    typeName = PrimitiveTypeMap[primitiveTypeReference.Type];
                    requiresNullabilityMarker = !ReferenceTypes.Contains(primitiveTypeReference.Type);
                    break;

                case SchemaTypeReference schemaTypeReference:
                    typeName = schemaTypeReference.Key;
                    requiresNullabilityMarker = schemaTypeReference.IsEnum(SchemaRegistry);
                    break;

                case null:
                    return "void";

                default:
                    throw new InvalidOperationException($"Unsupported result type: {reference.GetType()}");
            }

            if (reference.IsNullable/* && requiresNullabilityMarker*/)
                typeName = $"{typeName}?";

            if (reference.IsEnumerable) 
                typeName = WrapInEnumerable(typeName, enumerableBehavior);

            return typeName;
        }

        public string WrapInEnumerable(string typeName, EnumerableBehavior enumerableBehavior = EnumerableBehavior.Enumerable)
        {
            switch (enumerableBehavior)
            {
                case EnumerableBehavior.None: 
                    return typeName;

                case EnumerableBehavior.Enumerable:
                    this.AddUsing<IEnumerable<object>>();
                    return $"{nameof(IEnumerable<object>)}<{typeName}>";

                case EnumerableBehavior.Collection:
                    this.AddUsing<IReadOnlyList<object>>();
                    return $"{nameof(IReadOnlyList<object>)}<{typeName}>";

                default:
                    throw new ArgumentOutOfRangeException(nameof(enumerableBehavior), enumerableBehavior, null);
            }
        }

        public string NormalizeApiParameterName(string apiParameterName)
        {
            // Headers like 'Accept-Language'
            string normalized = Regex.Replace(apiParameterName, "[-]", String.Empty);
            normalized = StringExtensions.ToCamelCase(normalized);
            return normalized;
        }

        public CSharpValue BuildDefaultValueLiteral(ValueReference defaultValue)
        {
            switch (defaultValue)
            {
                case NullValueReference _:
                    return new CSharpValue("null");

                case PrimitiveValueReference primitiveValueReference:
                    return BuildDefaultValueLiteral(primitiveValueReference.Type.Type, primitiveValueReference.Value);

                case EnumMemberNumericReference enumMemberNumericReference:
                {
                    EnumSchemaMember member = enumMemberNumericReference.GetEnumMember(SchemaRegistry, _logger);
                    return new CSharpValue($"{member.Enum.FullName}.{member.Name}");
                }

                case EnumMemberStringReference enumMemberStringReference:
                {
                    EnumSchemaMember member = enumMemberStringReference.GetEnumMember(SchemaRegistry, _logger);
                    return new CSharpValue($"{member.Enum.FullName}.{member.Name}");
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue, $"Unexpected default value reference: {defaultValue?.GetType()}");
            }
        }

        internal void SetScopeName(string name)
        {
            Guard.IsNotNullOrEmpty(name, nameof(name));
            _currentNamespace = $"{_rootNamespace}.{name}";
        }

        private static CSharpValue BuildDefaultValueLiteral(PrimitiveType type, object value)
        {
            string strValue = value.ToString();
            switch (type)
            {
                case PrimitiveType.Boolean:
                    return new CSharpValue(strValue.ToLowerInvariant());

                case PrimitiveType.Byte:
                case PrimitiveType.Int16:
                case PrimitiveType.Int32:
                case PrimitiveType.Int64:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.Decimal: 
                    return new CSharpValue(strValue);

                case PrimitiveType.String:
                    return new CSharpStringValue(strValue);

                default: 
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}