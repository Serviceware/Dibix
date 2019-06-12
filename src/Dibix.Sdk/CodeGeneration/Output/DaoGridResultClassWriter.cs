using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoGridResultClassWriter : IDaoWriter
    {
        #region Fields
        private const string ComplexResultTypeSuffix = "Result";
        #endregion

        #region Properties
        public string RegionName => "Grid result types";
        #endregion

        #region IDaoWriter Members
        public bool HasContent(OutputConfiguration configuration, SourceArtifacts artifacts) => artifacts.Statements.Any(x => IsGridResult(configuration, x));

        public void Write(DaoWriterContext context)
        {
            var namespaceGroups = context.Artifacts
                                         .Statements
                                         .Where(x => IsGridResult(context.Configuration, x))
                                         .GroupBy(x => x.Namespace)
                                         .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SqlStatementInfo> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                IList<SqlStatementInfo> statements = namespaceGroup.ToArray();
                for (int j = 0; j < statements.Count; j++)
                {
                    SqlStatementInfo statement = statements[j];
                    CSharpModifiers classVisibility = context.Configuration.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
                    CSharpClass complexType = scope.AddClass(GetComplexTypeName(statement), classVisibility | CSharpModifiers.Sealed);

                    IList<SqlQueryResult> collectionProperties = statement.Results.Where(x => x.ResultMode == SqlQueryResultMode.Many).ToArray();

                    foreach (SqlQueryResult result in statement.Results)
                    {
                        bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
                        string propertyTypeName;
                        if (result.ResultTypeName != null)
                            propertyTypeName = MakeCollectionInterfaceType(result.ResultTypeName);
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
                        context.Output.AddUsing(typeof(ICollection<>).Namespace);
                        context.Output.AddUsing(typeof(Collection<>).Namespace);
                    }

                    StringBuilder ctorBodyWriter = new StringBuilder();
                    for (int k = 0; k < collectionProperties.Count; k++)
                    {
                        SqlQueryResult property = collectionProperties[k];
                        string collectionTypeName = MakeCollectionType(property.ResultTypeName ?? property.Contracts.First().Name.ToString());
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
        private static bool IsGridResult(OutputConfiguration configuration, SqlStatementInfo statement)
        {
            return statement.Results.Count > 1 && (statement.ResultTypeName == null || configuration.GeneratePublicArtifacts);
        }

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
            return statement.ResultTypeName ?? String.Concat(statement.Name, ComplexResultTypeSuffix);
        }
        #endregion
    }
}