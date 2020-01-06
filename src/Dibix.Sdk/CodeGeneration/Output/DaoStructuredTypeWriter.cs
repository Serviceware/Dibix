using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoStructuredTypeWriter : IDaoChildWriter
    {
        #region Properties
        public string LayerName => CodeGeneration.LayerName.Data;
        public string RegionName => "Structured types";
        #endregion

        #region IDaoChildWriter Members
        public bool HasContent(SourceArtifacts artifacts) => artifacts.UserDefinedTypes.Any();

        public IEnumerable<string> GetGlobalAnnotations(OutputConfiguration context) { yield break; }

        public void Write(WriterContext context)
        {
            context.AddUsing("Dibix");

            var namespaceGroups = context.Artifacts
                                         .UserDefinedTypes
                                         .GroupBy(x => context.Configuration.WriteNamespaces ? x.Namespace.RelativeNamespace : null)
                                         .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, UserDefinedTypeDefinition> namespaceGroup = namespaceGroups[i];
                IList<UserDefinedTypeDefinition> userDefinedTypes = namespaceGroup.ToArray();
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                for (int j = 0; j < userDefinedTypes.Count; j++)
                {
                    UserDefinedTypeDefinition userDefinedType = userDefinedTypes[j];
                    CSharpClass @class = scope.AddClass(userDefinedType.DisplayName, CSharpModifiers.Public | CSharpModifiers.Sealed)
                                              .Inherits($"StructuredType<{userDefinedType.DisplayName}, {String.Join(", ", userDefinedType.Columns.Select(x => x.Type))}>");

                    @class.AddConstructor(body: $"base.ImportSqlMetadata(() => this.Add({String.Join(", ", userDefinedType.Columns.Select(x => "default"))}));"
                                        , baseConstructorParameters: $"\"{userDefinedType.TypeName}\"");

                    CSharpMethod method = @class.AddMethod("Add", "void", $"base.AddValues({String.Join(", ", userDefinedType.Columns.Select(x => x.Name))});");
                    foreach (UserDefinedTypeColumn column in userDefinedType.Columns)
                        method.AddParameter(column.Name, column.Type);

                    if (j + 1 < userDefinedTypes.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < context.Artifacts.UserDefinedTypes.Count)
                    scope.AddSeparator();
            }
        }
        #endregion
    }
}