using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoGridResultClassWriter : DaoWriter
    {
        #region Properties
        public override string LayerName => CodeGeneration.LayerName.DomainModel;
        public override string RegionName => "Grid result types";
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Statements.Any(IsGridResult);

        public override void Write(DaoCodeGenerationContext context)
        {
            var namespaceGroups = context.Model
                                         .Statements
                                         .Where(IsGridResult)
                                         .GroupBy(x => context.WriteNamespaces ? x.GridResultType.Namespace.RelativeNamespace : null)
                                         .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SqlStatementInfo> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                IList<SqlStatementInfo> statements = namespaceGroup.DistinctBy(x => x.GridResultType.TypeName ?? x.Name).ToArray();
                for (int j = 0; j < statements.Count; j++)
                {
                    SqlStatementInfo statement = statements[j];
                    CSharpModifiers classVisibility = context.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
                    CSharpClass complexType = scope.AddClass(GetComplexTypeName(statement), classVisibility | CSharpModifiers.Sealed);

                    IList<SqlQueryResult> collectionProperties = statement.Results.Where(x => x.ResultMode == SqlQueryResultMode.Many).ToArray();

                    foreach (SqlQueryResult result in statement.Results)
                    {
                        bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
                        string propertyTypeName;
                        if (result.ResultType != null)
                            propertyTypeName = MakeCollectionInterfaceType(result.ResultType.ToString());
                        else
                        {
                            propertyTypeName = result.Contracts.First().Name.ToString();
                            if (isEnumerable)
                                propertyTypeName = MakeCollectionInterfaceType(propertyTypeName);
                        }

                        complexType.AddProperty(result.Name, propertyTypeName)
                                   .Getter(null)
                                   .Setter(null, isEnumerable ? CSharpModifiers.Private : default);
                    }

                    if (!collectionProperties.Any())
                        continue;

                    if (collectionProperties.Any())
                    {
                        context.AddUsing(typeof(ICollection<>).Namespace)
                               .AddUsing(typeof(Collection<>).Namespace);
                    }

                    StringBuilder ctorBodyWriter = new StringBuilder();
                    for (int k = 0; k < collectionProperties.Count; k++)
                    {
                        SqlQueryResult property = collectionProperties[k];
                        string innerTypeName = property.ResultType?.ToString() ?? property.Contracts.First().Name.ToString();
                        string collectionTypeName = MakeCollectionType(innerTypeName);
                        ctorBodyWriter.Append("this.")
                                      .Append(property.Name)
                                      .Append(" = new ")
                                      .Append(collectionTypeName)
                                      .Append("();");

                        if (k + 1 < collectionProperties.Count)
                            ctorBodyWriter.AppendLine();
                    }

                    complexType.AddSeparator()
                               .AddConstructor(ctorBodyWriter.ToString());

                    if (j + 1 < statements.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < namespaceGroups.Length)
                    context.Output.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static bool IsGridResult(SqlStatementInfo statement) => statement.GridResultType != null;

        private static string MakeCollectionInterfaceType(string typeName)
        {
            return String.Concat("ICollection<", typeName, '>');
        }

        private static string MakeCollectionType(string typeName)
        {
            return String.Concat("Collection<", typeName, '>');
        }

        private static string GetComplexTypeName(SqlStatementInfo statement)
        {
            if (statement.ResultType != null)
                return statement.ResultType.ToString();

            if (statement.GridResultType.TypeName != null)
                return statement.GridResultType.TypeName;

            throw new InvalidOperationException($"Statement '{statement.Name}' has no result type");
        }
        #endregion
    }
}