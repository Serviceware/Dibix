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
        public bool HasContent(IEnumerable<SqlStatementInfo> statements) => statements.Any(IsGridResult);

        public void Write(DaoWriterContext context)
        {
            IList<SqlStatementInfo> gridResultStatements = context.Artifacts.Statements.Where(IsGridResult).ToArray();
            for (int i = 0; i < gridResultStatements.Count; i++)
            {
                SqlStatementInfo statement = gridResultStatements[i];
                CSharpClass complexType = context.Output.AddClass(GetComplexTypeName(statement), CSharpModifiers.Internal | CSharpModifiers.Sealed);

                IList<SqlQueryResult> collectionProperties =
                    statement.Results.Where(x => x.ResultMode == SqlQueryResultMode.Many).ToArray();

                foreach (SqlQueryResult result in statement.Results)
                {
                    bool isEnumerable = result.ResultMode == SqlQueryResultMode.Many;
                    string resultTypeName = result.Contracts.First().Name.ToString();
                    if (isEnumerable)
                        resultTypeName = MakeCollectionInterfaceType(resultTypeName);

                    complexType.AddProperty(result.Name, resultTypeName)
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
                for (int j = 0; j < collectionProperties.Count; j++)
                {
                    SqlQueryResult property = collectionProperties[j];
                    ctorBodyWriter.Append("this.")
                                  .Append(property.Name)
                                  .Append(" = new ")
                                  .Append(MakeCollectionType(property.Contracts.First().Name.ToString()))
                                  .Append("();");

                    if (j + 1 < collectionProperties.Count)
                        ctorBodyWriter.AppendLine();
                }

                complexType.AddSeparator()
                           .AddConstructor(ctorBodyWriter.ToString());

                if (i + 1 < gridResultStatements.Count)
                    context.Output.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static bool IsGridResult(SqlStatementInfo x) => x.Results.Count > 1 && x.ResultTypeName == null;

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