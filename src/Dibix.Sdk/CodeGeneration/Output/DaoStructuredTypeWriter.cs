using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoStructuredTypeWriter : DaoWriter
    {
        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Data;
        public override string RegionName => "Structured types";
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.UserDefinedTypes.Any();

        public override IEnumerable<string> GetGlobalAnnotations(CodeGenerationModel model) { yield break; }

        public override void Write(DaoCodeGenerationContext context)
        {
            context.AddUsing("Dibix");

            var namespaceGroups = context.Model
                                         .UserDefinedTypes
                                         .GroupBy(x => context.WriteNamespaces ? NamespaceUtility.BuildRelativeNamespace(context.Model.RootNamespace, this.LayerName, x.Namespace) : null)
                                         .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, UserDefinedTypeSchema> namespaceGroup = namespaceGroups[i];
                IList<UserDefinedTypeSchema> userDefinedTypes = namespaceGroup.ToArray();
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                for (int j = 0; j < userDefinedTypes.Count; j++)
                {
                    UserDefinedTypeSchema userDefinedType = userDefinedTypes[j];
                    CSharpClass @class = scope.AddClass(userDefinedType.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed)
                                              .Inherits($"StructuredType<{userDefinedType.DefinitionName}, {String.Join(", ", userDefinedType.Properties.Select(x => context.ResolveTypeName(x.Type)))}>");

                    @class.AddConstructor(body: $"base.ImportSqlMetadata(() => this.Add({String.Join(", ", userDefinedType.Properties.Select(x => "default"))}));"
                                        , baseConstructorParameters: $"\"{userDefinedType.UdtTypeName}\"");

                    CSharpMethod method = @class.AddMethod("Add", "void", $"base.AddValues({String.Join(", ", userDefinedType.Properties.Select(x => x.Name))});");
                    foreach (ObjectSchemaProperty column in userDefinedType.Properties)
                        method.AddParameter(column.Name, context.ResolveTypeName(column.Type));

                    if (j + 1 < userDefinedTypes.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < context.Model.UserDefinedTypes.Count)
                    scope.AddSeparator();
            }
        }
        #endregion
    }
}