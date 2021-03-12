using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DaoCodeGenerationContext
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
            if (reference.IsNullable/* && requiresNullabilityMarker*/)
                sb.Append('?');

            return sb.ToString();
        }

        public string GetRelativeNamespace(string layerName, string relativeNamespace)
        {
            if (!this.WriteNamespaces)
                return null;
                
            return NamespaceUtility.BuildRelativeNamespace(this.Model.RootNamespace, layerName, relativeNamespace);
        }
    }
}