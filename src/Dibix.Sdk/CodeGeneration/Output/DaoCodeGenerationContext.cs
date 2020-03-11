using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DaoCodeGenerationContext
    {
        private static readonly IDictionary<PrimitiveDataType, string> PrimitiveTypeMap = new Dictionary<PrimitiveDataType, string>
        {
            [PrimitiveDataType.Boolean]        = "bool"
          , [PrimitiveDataType.Byte]           = "byte"
          , [PrimitiveDataType.Int16]          = "short"
          , [PrimitiveDataType.Int32]          = "int"
          , [PrimitiveDataType.Int64]          = "long"
          , [PrimitiveDataType.Float]          = "float"
          , [PrimitiveDataType.Double]         = "double"
          , [PrimitiveDataType.Decimal]        = "decimal"
          , [PrimitiveDataType.Binary]         = "byte[]"
          , [PrimitiveDataType.DateTime]       = "System.DateTime"
          , [PrimitiveDataType.DateTimeOffset] = "System.DateTimeOffset"
          , [PrimitiveDataType.String]         = "string"
          , [PrimitiveDataType.UUID]           = "System.Guid"
          , [PrimitiveDataType.Xml]            = "System.Xml.Linq.XElement"
        };
        private static readonly PrimitiveDataType[] ReferenceTypes =
        {
            PrimitiveDataType.Binary
          , PrimitiveDataType.String
          , PrimitiveDataType.Xml
        };
        private readonly CSharpRoot _root;
        private readonly ISchemaRegistry _schemaRegistry;

        public CSharpStatementScope Output { get; internal set; }
        public string GeneratedCodeAnnotation { get; }
        public CodeGenerationModel Model { get; }
        public bool WriteGuardChecks { get; set; }
        public bool GeneratePublicArtifacts => this.Model.CompatibilityLevel == CodeGeneratorCompatibilityLevel.Full;
        public bool WriteNamespaces => this.Model.CompatibilityLevel == CodeGeneratorCompatibilityLevel.Full;

        internal DaoCodeGenerationContext(CSharpRoot root, string generatedCodeAnnotation, CodeGenerationModel model, ISchemaRegistry schemaRegistry)
        {
            this._root = root;
            this._schemaRegistry = schemaRegistry;
            this.Output = root;
            this.GeneratedCodeAnnotation = generatedCodeAnnotation;
            this.Model = model;
        }

        public DaoCodeGenerationContext AddUsing(string @using)
        {
            this._root.AddUsing(@using);
            return this;
        }

        public SchemaDefinition GetSchema(SchemaTypeReference reference) => this._schemaRegistry.GetSchema(reference);

        public string ResolveTypeName(TypeReference reference)
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

                default:
                    throw new InvalidOperationException($"Unsupported result type: {reference.GetType()}");
            }

            StringBuilder sb = new StringBuilder(typeName);
            if (reference.IsNullable && requiresNullabilityMarker)
                sb.Append('?');

            return sb.ToString();
        }
    }
}