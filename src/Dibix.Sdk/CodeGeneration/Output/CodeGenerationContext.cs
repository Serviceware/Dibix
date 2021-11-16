using System;
using System.Collections.Generic;
using System.Linq;
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

        public CSharpStatementScope Output { get; internal set; }
        public CodeGenerationModel Model { get; }
        public ISchemaRegistry SchemaRegistry { get; }
        public bool WriteGuardChecks { get; set; }
        public bool GeneratePublicArtifacts => this.Model.CompatibilityLevel == CodeGeneratorCompatibilityLevel.Full;
        public bool WriteNamespaces => this.Model.CompatibilityLevel == CodeGeneratorCompatibilityLevel.Full;

        internal CodeGenerationContext(CSharpRoot root, CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            this._root = root;
            this.Output = root;
            this.Model = model;
            this.SchemaRegistry = schemaRegistry;
            this._logger = logger;
        }

        public CodeGenerationContext AddUsing(string @using)
        {
            this._root.AddUsing(@using);
            return this;
        }

        public SchemaDefinition GetSchema(SchemaTypeReference reference) => this.SchemaRegistry.GetSchema(reference);

        public string ResolveTypeName(TypeReference reference, CodeGenerationContext context, bool includeEnumerable = true)
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
                    requiresNullabilityMarker = this.GetSchema(schemaTypeReference) is EnumSchema;
                    break;

                case null:
                    return "void";

                default:
                    throw new InvalidOperationException($"Unsupported result type: {reference.GetType()}");
            }

            if (reference.IsNullable/* && requiresNullabilityMarker*/)
                typeName = $"{typeName}?";

            if (reference.IsEnumerable && includeEnumerable) 
                typeName = this.WrapInEnumerable(typeName, context);

            return typeName;
        }

        public string WrapInEnumerable(string typeName, CodeGenerationContext context)
        {
            context.AddUsing<IEnumerable<object>>();
            return $"{nameof(IEnumerable<object>)}<{typeName}>";
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
                    EnumSchemaMember member = enumMemberNumericReference.GetEnumMember(this.SchemaRegistry, this._logger);
                    return new CSharpValue($"{member.Enum.FullName}.{member.Name}");
                }

                case EnumMemberStringReference enumMemberStringReference:
                {
                    EnumSchemaMember member = enumMemberStringReference.GetEnumMember(this.SchemaRegistry, this._logger);
                    return new CSharpValue($"{member.Enum.FullName}.{member.Name}");
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue, $"Unexpected default value reference: {defaultValue?.GetType()}");
            }
        }

        public string GetRelativeNamespace(string layerName, string relativeNamespace)
        {
            if (!this.WriteNamespaces)
                return null;
                
            return NamespaceUtility.BuildRelativeNamespace(this.Model.RootNamespace, layerName, relativeNamespace);
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