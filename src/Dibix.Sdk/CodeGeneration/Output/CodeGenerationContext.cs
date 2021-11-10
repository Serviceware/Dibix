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

        public CSharpStatementScope Output { get; internal set; }
        public CodeGenerationModel Model { get; }
        public ISchemaRegistry SchemaRegistry { get; }
        public bool WriteGuardChecks { get; set; }
        public bool GeneratePublicArtifacts => this.Model.CompatibilityLevel == CodeGeneratorCompatibilityLevel.Full;
        public bool WriteNamespaces => this.Model.CompatibilityLevel == CodeGeneratorCompatibilityLevel.Full;

        internal CodeGenerationContext(CSharpRoot root, CodeGenerationModel model, ISchemaRegistry schemaRegistry)
        {
            this._root = root;
            this.Output = root;
            this.Model = model;
            this.SchemaRegistry = schemaRegistry;
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

        public CSharpValue BuildDefaultValueLiteral(DefaultValue defaultValue)
        {
            object value = defaultValue.Value;
            string defaultValueStr = value?.ToString();
            switch (value)
            {
                case bool _: return new CSharpValue(defaultValueStr.ToLowerInvariant());
                case char defaultValueChar: return new CSharpCharacterValue(defaultValueChar);
                case string _: return new CSharpStringValue(defaultValueStr);
                case EnumSchemaMember enumMember: return new CSharpValue($"{enumMember.Enum.FullName}.{enumMember.Name}");
                case null: return new CSharpValue("null");
                default: return new CSharpValue(defaultValueStr);
            }
        }

        public string GetRelativeNamespace(string layerName, string relativeNamespace)
        {
            if (!this.WriteNamespaces)
                return null;
                
            return NamespaceUtility.BuildRelativeNamespace(this.Model.RootNamespace, layerName, relativeNamespace);
        }
    }
}