using System;
using System.Collections.Generic;
using System.Linq;
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

            var namespaceGroups = _schemas.GroupBy(x => x.Namespace).OrderBy(x => x.Key).ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, UserDefinedTypeSchema> namespaceGroup = namespaceGroups[i];
                IList<UserDefinedTypeSchema> userDefinedTypes = namespaceGroup.OrderBy(x => x.DefinitionName).ToArray();
                CSharpStatementScope scope = /*namespaceGroup.Key != null ? */context.CreateOutputScope(namespaceGroup.Key)/* : context.Output*/;
                for (int j = 0; j < userDefinedTypes.Count; j++)
                {
                    UserDefinedTypeSchema userDefinedType = userDefinedTypes[j];
                    CSharpClass @class = scope.AddClass(userDefinedType.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed, new CSharpAnnotation("StructuredType", new CSharpStringValue(userDefinedType.UdtName)))
                                              .Inherits($"StructuredType<{userDefinedType.DefinitionName}, {String.Join(", ", userDefinedType.Properties.Select(x => context.ResolveTypeName(x.Type)))}>");

                    @class.AddConstructor(body: $"base.ImportSqlMetadata(() => Add({String.Join(", ", userDefinedType.Properties.Select(x => "default"))}));")
                          .CallBase()
                          .AddParameter(new CSharpStringValue(userDefinedType.UdtName));

                    CSharpMethod method = @class.AddMethod("Add", "void", $"base.AddValues({String.Join(", ", userDefinedType.Properties.Select(x => x.Name.Value))});");
                    foreach (ObjectSchemaProperty column in userDefinedType.Properties)
                        method.AddParameter(column.Name, context.ResolveTypeName(column.Type));

                    if (j + 1 < userDefinedTypes.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < namespaceGroups.Length)
                    context.AddSeparator();
            }
        }
        #endregion
    }
}