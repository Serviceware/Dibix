using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoStructuredTypeWriter : ArtifactWriterBase
    {
        #region Fields
        private readonly ICollection<UserDefinedTypeSchema> _schemas;
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Data;
        public override string RegionName => "Structured types";
        #endregion

        #region Constructor
        public DaoStructuredTypeWriter(CodeGenerationModel model, CodeGenerationOutputFilter outputFilter)
        {
            _schemas = model.GetSchemas(outputFilter)
                            .OfType<UserDefinedTypeSchema>()
                            .ToArray();
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => _schemas.Any();

        public override IEnumerable<CSharpAnnotation> GetGlobalAnnotations(CodeGenerationModel model) { yield break; }

        public override void Write(CodeGenerationContext context)
        {
            context.AddUsing("Dibix");
            context.AddUsing<SqlDbType>();

            var namespaceGroups = _schemas.GroupBy(x => x.AbsoluteNamespace).OrderBy(x => x.Key).ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, UserDefinedTypeSchema> namespaceGroup = namespaceGroups[i];
                IList<UserDefinedTypeSchema> userDefinedTypes = namespaceGroup.OrderBy(x => x.DefinitionName).ToArray();
                CSharpStatementScope scope = /*namespaceGroup.Key != null ? */context.CreateOutputScope(namespaceGroup.Key)/* : context.Output*/;
                for (int j = 0; j < userDefinedTypes.Count; j++)
                {
                    UserDefinedTypeSchema userDefinedType = userDefinedTypes[j];
                    CSharpClass @class = scope.AddClass(userDefinedType.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed, new CSharpAnnotation("StructuredType", new CSharpStringValue(userDefinedType.UdtName)))
                                              .Inherits($"StructuredType<{userDefinedType.DefinitionName}>");

                    @class.AddProperty("TypeName", "string", CSharpModifiers.Public | CSharpModifiers.Override)
                          .Getter($"return \"{userDefinedType.UdtName}\";");

                    CSharpMethod addMethod = @class.AddMethod("Add", "void", $"AddRecord({String.Join(", ", userDefinedType.Properties.Select(x => x.Name.Value))});");
                    foreach (ObjectSchemaProperty column in userDefinedType.Properties)
                        addMethod.AddParameter(column.Name, context.ResolveTypeName(column.Type));

                    string collectMetadataBody = String.Join(Environment.NewLine, userDefinedType.Properties.Select(CollectRegisterMetadataStatement));
                    @class.AddMethod("CollectMetadata", "void", collectMetadataBody, CSharpModifiers.Protected | CSharpModifiers.Override)
                          .AddParameter("collector", "ISqlMetadataCollector");

                    if (j + 1 < userDefinedTypes.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < namespaceGroups.Length)
                    context.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static string CollectRegisterMetadataStatement(ObjectSchemaProperty property)
        {
            SqlDbType sqlDbType = GetSqlDbType(property.Type);
            StringBuilder sb = new StringBuilder($"collector.RegisterMetadata(\"{property.Name}\", SqlDbType.{sqlDbType}");

            if (sqlDbType == SqlDbType.Decimal)
            {
                const byte defaultPrecision = 19;
                const byte defaultScale = 2;
                byte? precision = null; // TODO: Collect from UDT
                byte? scale = null; // TODO: Collect from UDT
                sb.Append($", {precision ?? defaultPrecision}, {scale ?? defaultScale}");
            }
            else if (sqlDbType is SqlDbType.NVarChar or SqlDbType.VarBinary)
            {
                const int defaultMaxLength = -1;
                int? maxLength = null; // TODO: Collect from UDT
                sb.Append($", {maxLength ?? defaultMaxLength}");
            }

            sb.Append(");");

            string statement = sb.ToString();
            return statement;
        }

        private static SqlDbType GetSqlDbType(TypeReference typeReference)
        {
            if (typeReference is not PrimitiveTypeReference primitiveTypeReference)
                throw new ArgumentOutOfRangeException(nameof(typeReference), typeReference, null);

            return PrimitiveTypeMap.GetSqlDbType(primitiveTypeReference.Type);
        }
        #endregion
    }
}