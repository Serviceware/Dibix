using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class StatementOutputParser
    {
        private const string ReturnHintClrTypes = "ClrTypes";
        private const string ReturnHintMode = "Mode";
        private const string ReturnHintResultName = "Name";
        private const string ReturnHintSplitOn = "SplitOn";
        private const string ReturnHintConverter = "Converter";

        public static IEnumerable<SqlQueryResult> Parse(IExecutionEnvironment environment, string sourcePath, TSqlStatement node)
        {
            StatementOutputVisitor visitor = new StatementOutputVisitor(environment, sourcePath);
            node.Accept(visitor);

            IList<SqlHint> returnHints = SqlHintReader.Read(node)
                                                      .Where(x => x.Kind == SqlHint.Return)
                                                      .ToArray();

            if (returnHints.Count > visitor.Results.Count)
            {
                environment.RegisterError(sourcePath, node.StartLine, node.StartColumn, null, "There are more return declarations than output statements being produced by the statement");
                yield break;
            }

            if (returnHints.Count < visitor.Results.Count)
            {
                environment.RegisterError(sourcePath, node.StartLine, node.StartColumn, null, "There are missing return declarations for the output statements. Please mark the header of the statement with a line per output containting this hint: -- @Return <ClrTypeName>");
                yield break;
            }

            HashSet<string> usedOutputNames = new HashSet<string>();

            for (int i = 0; i < returnHints.Count; i++)
            {
                SqlHint returnHint = returnHints[i];
                if (!returnHint.TrySelectValueOrContent(ReturnHintClrTypes, x => environment.RegisterError(sourcePath, node.StartLine, node.StartColumn, null, x), out var typeNamesStr))
                    yield break;

                string[] typeNames = typeNamesStr.Split(';');
                SqlQueryResultMode resultMode = returnHint.SelectValueOrDefault(ReturnHintMode, x => (SqlQueryResultMode)Enum.Parse(typeof(SqlQueryResultMode), x));
                string resultName = returnHint.SelectValueOrDefault(ReturnHintResultName);
                string converter = returnHint.SelectValueOrDefault(ReturnHintConverter);
                string splitOn = returnHint.SelectValueOrDefault(ReturnHintSplitOn);

                SqlQueryResult result = new SqlQueryResult
                {
                    Name = resultName,
                    ResultMode = resultMode,
                    Converter = converter,
                    SplitOn = splitOn
                };

                OutputSelectResult output = visitor.Results[i];
                IList<TypeInfo> returnTypes = typeNames.Select(x => TypeLoaderFacade.LoadType(x, environment, y => environment.RegisterError(sourcePath, returnHint.Line, returnHint.Column, null, y))).ToArray();
                if (returnTypes.Any(x => x == null))
                    continue;
                
                ValidateResult(environment, returnHints.Count, returnHint, result, returnTypes, output.Columns, usedOutputNames, sourcePath);

                result.Types.AddRange(returnTypes);
                result.Columns.AddRange(output.Columns.Select(x => x.ColumnName));

                yield return result;
            }
        }

        private static void ValidateResult(IExecutionEnvironment environment, int numberOfReturnHints, SqlHint returnHint, SqlQueryResult result, IList<TypeInfo> returnTypes, IEnumerable<OutputColumnResult> columns, HashSet<string> usedOutputNames, string sourcePath)
        {
            // Validate return count/name
            if (!String.IsNullOrEmpty(result.Name))
            {
                if (usedOutputNames.Contains(result.Name))
                    environment.RegisterError(sourcePath, returnHint.Line, returnHint.Column, null, $"The name '{result.Name}' is already defined for another output result");
                else if (numberOfReturnHints == 1)
                    environment.RegisterError(sourcePath, returnHint.Line, returnHint.Column, null, "The 'Name' property is irrelevant when a single output is returned");
                else
                    usedOutputNames.Add(result.Name);
            }
            else if (numberOfReturnHints > 1)
                environment.RegisterError(sourcePath, returnHint.Line, returnHint.Column, null, "The 'Name' property must be specified when multiple outputs are returned. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName>");

            // Validate required properties for MultiMap
            if (returnTypes.Count > 1)
            {
                if (String.IsNullOrEmpty(result.SplitOn))
                {
                    environment.RegisterError(sourcePath, returnHint.Line, returnHint.Column, null, "The 'SplitOn' property must be specified when using multiple return types. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName> SplitOn:<SplitColumnName>");
                    return;
                }

                if (String.IsNullOrEmpty(result.Converter))
                {
                    environment.RegisterError(sourcePath, returnHint.Line, returnHint.Column, null, "The 'Converter' property must be specified when using multiple return types. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName> SplitOn:<SplitColumnName> Converter:<ClrMethodDelegate>");
                }
            }

            // Validate if the return statements match the return types
            IList<ICollection<OutputColumnResult>> columnGroups = SplitColumns(columns, result.SplitOn).ToArray();
            if (columnGroups.Count > 1 && columnGroups.Any(x => !x.Any()))
            {
                environment.RegisterError(sourcePath, returnHint.Line, returnHint.Column, null, "Part of the SplitOn property value did not match a column in the output expression");
                return;
            }
            if (columnGroups.Count != returnTypes.Count)
            {
                environment.RegisterError(sourcePath, returnHint.Line, returnHint.Column, null, "The SplitOn property does not match the number of return types");
                return;
            }

            for (int i = 0; i < returnTypes.Count; i++)
            {
                TypeInfo returnType = returnTypes[i];
                ICollection<OutputColumnResult> columnGroup = columnGroups[i];

                bool singleColumn = columnGroup.Count == 1;
                bool scalarResult = result.ResultMode == SqlQueryResultMode.Scalar || returnType.IsPrimitiveType;

                // SELECT 1 => Query<int>/ExecuteScalar => No entity property validation + No missing alias validation
                if (singleColumn && scalarResult)
                    return;

                foreach (OutputColumnResult columnResult in columnGroup)
                {
                    // Validate alias
                    // i.E.: SELECT COUNT(*) no alias
                    if (!columnResult.Result)
                    {
                        environment.RegisterError(sourcePath, columnResult.Line, columnResult.Column, null, $@"Missing alias for expression '{columnResult.Expression}'");
                        continue;
                    }

                    // Validate if entity property exists
                    if (returnType.Properties.All(x => !String.Equals(x, columnResult.ColumnName, StringComparison.OrdinalIgnoreCase)))
                        environment.RegisterError(sourcePath, columnResult.Line, columnResult.Column, null, $@"Property '{columnResult.ColumnName}' not found on return type '{returnType.Name}'");
                }
            }
        }

        private static IEnumerable<ICollection<OutputColumnResult>> SplitColumns(IEnumerable<OutputColumnResult> columns, string splitOn)
        {
            Queue<OutputColumnResult> columnQueue = new Queue<OutputColumnResult>(columns);
            foreach (string currentSplitOn in (splitOn ?? String.Empty).Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                ICollection<OutputColumnResult> results = SplitColumns(columnQueue, currentSplitOn).ToArray();
                yield return results;
            }
            yield return columnQueue.ToArray();
        }

        private static IEnumerable<OutputColumnResult> SplitColumns(Queue<OutputColumnResult> columnQueue, string currentSplitOn)
        {
            bool firstColumn = true;
            while (true)
            {
                if (!columnQueue.Any())
                    break;

                // Don't start splitting until we at least iterated one column
                if (firstColumn)
                    firstColumn = false;
                else if (String.Equals(columnQueue.Peek().ColumnName, currentSplitOn, StringComparison.OrdinalIgnoreCase))
                    break;

                yield return columnQueue.Dequeue();
            }
        }
    }
}