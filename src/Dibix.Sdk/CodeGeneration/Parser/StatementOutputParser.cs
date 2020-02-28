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
        private const string ReturnHintResultType = "ResultType";

        public static IEnumerable<SqlQueryResult> Parse(SqlStatementInfo target, TSqlFragment node, ICollection<SqlHint> hints, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            StatementOutputVisitor visitor = new StatementOutputVisitor(target.Source, errorReporter);
            node.Accept(visitor);

            IList<SqlHint> returnHints = hints.Where(x => x.Kind == SqlHint.Return).ToArray();

            ValidateFileApi(target, node, returnHints, visitor.Results, errorReporter);
            ValidateMergeGridResult(target, node, returnHints, errorReporter);

            // Incorrect number of return hints/results will make further execution fail
            if (!ValidateReturnHints(target, node, returnHints, visitor.Results, errorReporter)) 
                yield break;

            foreach (SqlQueryResult result in CollectResults(target, node, contractResolver, errorReporter, returnHints, visitor)) 
                yield return result;
        }

        private static void ValidateFileApi(SqlStatementInfo target, TSqlFragment node, IEnumerable<SqlHint> returnHints, ICollection<OutputSelectResult> results, IErrorReporter errorReporter)
        {
            if (!target.IsFileApi) 
                return;
            
            if (returnHints.Any() || results.Count != 1)
            {
                errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, "When using the @FileApi option, the query should return only one output statement and no return declarations should be defined");
            }

            if (target.MergeGridResult)
            {
                errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, "When using the @FileApi option, the @MergeGridResult option is invalid");
            }
        }

        private static void ValidateMergeGridResult(SqlStatementInfo target, TSqlFragment node, IList<SqlHint> returnHints, IErrorReporter errorReporter)
        {
            if (!target.MergeGridResult || returnHints.Count > 1) 
                return;

            errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, "The @MergeGridResult option only works with a grid result so at least two results should be specified with the @Return hint");
        }

        private static bool ValidateReturnHints(SqlStatementInfo target, TSqlFragment node, IList<SqlHint> returnHints, ICollection<OutputSelectResult> results, IErrorReporter errorReporter)
        {
            if (target.IsFileApi)
                return true;

            if (returnHints.Count > results.Count)
            {
                errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, "There are more return declarations than output statements being produced by the statement");
                return false;
            }

            if (returnHints.Count < results.Count)
            {
                errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, "There are missing return declarations for the output statements. Please mark the header of the statement with a line per output containting this hint: -- @Return <ClrTypeName>");
                return false;
            }

            return true;
        }

        private static IEnumerable<SqlQueryResult> CollectResults(SqlStatementInfo target, TSqlFragment node, IContractResolverFacade contractResolver, IErrorReporter errorReporter, IList<SqlHint> returnHints, StatementOutputVisitor visitor)
        {
            ICollection<string> usedOutputNames = new HashSet<string>();
            for (int i = 0; i < returnHints.Count; i++)
            {
                SqlHint returnHint = returnHints[i];
                if (!returnHint.TrySelectValueOrContent(ReturnHintClrTypes, x => errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, x), out string typeNamesStr))
                    yield break;

                if (!TryParseResultMode(target, node, returnHint, errorReporter, out SqlQueryResultMode resultMode))
                    yield break;

                string resultName = returnHint.SelectValueOrDefault(ReturnHintResultName);
                string converter = returnHint.SelectValueOrDefault(ReturnHintConverter);
                string splitOn = returnHint.SelectValueOrDefault(ReturnHintSplitOn);

                ValidateMergeGridResult(target, node, i == 0, resultMode, resultName, errorReporter);

                SqlQueryResult result = new SqlQueryResult
                {
                    Name = resultName,
                    ResultMode = resultMode,
                    Converter = converter,
                    SplitOn = splitOn,
                    ResultType = ParseResultType(target, node, returnHints, resultMode, returnHint, contractResolver, errorReporter)
                };

                if (!TryParseResultContracts(target, returnHint, typeNamesStr, contractResolver, errorReporter, out IEnumerable<ContractInfo> contracts))
                    continue;
                
                result.Contracts.AddRange(contracts);

                OutputSelectResult output = visitor.Results[i];
                ValidateResult(i == 0, returnHints.Count, returnHint, result, output.Columns, usedOutputNames, target, errorReporter);
                result.Columns.AddRange(output.Columns.Select(x => x.ColumnName));

                yield return result;
            }
        }

        private static bool TryParseResultMode(SqlStatementInfo target, TSqlFragment node, SqlHint returnHint, IErrorReporter errorReporter, out SqlQueryResultMode resultMode)
        {
            string resultModeStr = returnHint.SelectValueOrDefault(ReturnHintMode);
            resultMode = SqlQueryResultMode.Many;
            if (resultModeStr == null || Enum.TryParse(resultModeStr, out resultMode)) 
                return true;

            errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, $"Result mode not supported: {resultModeStr}");
            return false;
        }

        private static void ValidateMergeGridResult(SqlStatementInfo target, TSqlFragment node, bool isFirstResult, SqlQueryResultMode resultMode, string resultName, IErrorReporter errorReporter)
        {
            SqlQueryResultMode[] supportedMergeGridResultModes = { SqlQueryResultMode.Single, SqlQueryResultMode.SingleOrDefault };
            if (!target.MergeGridResult || !isFirstResult) 
                return;

            if (!supportedMergeGridResultModes.Contains(resultMode))
            {
                errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, $"When using the @MergeGridResult option, the first result should specify one of the following result modes using the 'Mode' property: {String.Join(", ", supportedMergeGridResultModes)}");
            }

            if (!String.IsNullOrEmpty(resultName))
            {
                errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, "When using the @MergeGridResult option, the 'Name' property must not be set on the first result");
            }
        }

        private static ContractName ParseResultType(SqlStatementInfo target, TSqlFragment node, ICollection<SqlHint> returnHints, SqlQueryResultMode resultMode, SqlHint returnHint, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            SqlQueryResultMode[] supportedResultTypeResultModes = { SqlQueryResultMode.Many };
            string resultTypeStr = returnHint.SelectValueOrDefault(ReturnHintResultType);
            if (String.IsNullOrEmpty(resultTypeStr)) 
                return null;

            bool singleResult = returnHints.Count <= 1;
            bool isResultTypeSupported = !supportedResultTypeResultModes.Contains(resultMode);

            if (singleResult)
            {
                // NOTE: Uncomment the Inline_SingleMultiMapResult_WithProjection test, whenever this is implemented
                errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, "Projection using the 'ResultType' property is currently only supported in a part of a grid result");
            }

            if (isResultTypeSupported)
            {
                errorReporter.RegisterError(target.Source, node.StartLine, node.StartColumn, null, $"Projection using the 'ResultType' property is currently only supported for the following result modes using the 'Mode' property: {String.Join(", ", supportedResultTypeResultModes)}");
            }

            if (singleResult || isResultTypeSupported) 
                return null;

            ContractInfo contract = contractResolver.ResolveContract(resultTypeStr, x => errorReporter.RegisterError(target.Source, returnHint.Line, returnHint.Column, null, x));
            return contract?.Name;
        }

        private static bool TryParseResultContracts(SqlStatementInfo target, SqlHint returnHint, string typeNamesStr, IContractResolverFacade contractResolver, IErrorReporter errorReporter, out IEnumerable<ContractInfo> contracts)
        {
            string[] typeNames = typeNamesStr.Split(';');
            IList<ContractInfo> returnTypes = typeNames.Select(x => contractResolver.ResolveContract(x, y => errorReporter.RegisterError(target.Source, returnHint.Line, returnHint.Column, null, y))).ToArray();
            if (returnTypes.All(x => x != null))
            {
                contracts = returnTypes;
                return true;
            }

            contracts = Enumerable.Empty<ContractInfo>();
            return false;
        }

        private static void ValidateResult
        (
            bool isFirstResult
          , int numberOfReturnHints
          , SqlHint returnHint
          , SqlQueryResult result
          , IEnumerable<OutputColumnResult> columns
          , ICollection<string> usedOutputNames
          , SqlStatementInfo target
          , IErrorReporter errorReporter
        )
        {
            ValidateName(isFirstResult, numberOfReturnHints, returnHint, result, usedOutputNames, target, errorReporter);

            IList<ICollection<OutputColumnResult>> columnGroups = SplitColumns(columns, result.SplitOn).ToArray();
            ValidateSplitOn(returnHint, result, columnGroups, target, errorReporter);

            for (int i = 0; i < result.Contracts.Count; i++)
            {
                ContractInfo returnType = result.Contracts[i];
                ICollection<OutputColumnResult> columnGroup = columnGroups[i];

                // SELECT 1 => Query<int>/Single<int> => No entity property validation + No missing alias validation
                bool singleColumn = columnGroup.Count == 1;
                if (singleColumn && returnType.IsPrimitiveType)
                    continue;

                foreach (OutputColumnResult columnResult in columnGroup)
                {
                    // Validate alias
                    // i.E.: SELECT COUNT(*) no alias
                    if (!columnResult.Result)
                    {
                        errorReporter.RegisterError(target.Source, columnResult.Line, columnResult.Column, null, $"Missing alias for expression '{columnResult.Expression}'");
                        continue;
                    }

                    // Validate if entity property exists
                    if (returnType.Properties.All(x => !String.Equals(x, columnResult.ColumnName, StringComparison.OrdinalIgnoreCase)))
                        errorReporter.RegisterError(target.Source, columnResult.Line, columnResult.Column, null, $"Property '{columnResult.ColumnName}' not found on return type '{returnType.Name}'");
                }
            }
        }

        private static void ValidateName(bool isFirstResult, int numberOfReturnHints, SqlHint returnHint, SqlQueryResult result, ICollection<string> usedOutputNames, SqlStatementInfo target, IErrorReporter errorReporter)
        {
            if (!String.IsNullOrEmpty(result.Name))
            {
                if (usedOutputNames.Contains(result.Name))
                    errorReporter.RegisterError(target.Source, returnHint.Line, returnHint.Column, null, $"The name '{result.Name}' is already defined for another output result");
                else if (numberOfReturnHints == 1)
                    errorReporter.RegisterError(target.Source, returnHint.Line, returnHint.Column, null, "The 'Name' property is irrelevant when a single output is returned");
                else
                    usedOutputNames.Add(result.Name);
            }
            else if (numberOfReturnHints > 1 && (!target.MergeGridResult || !isFirstResult))
                errorReporter.RegisterError(target.Source, returnHint.Line, returnHint.Column, null, "The 'Name' property must be specified when multiple outputs are returned. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName>");
        }

        private static void ValidateSplitOn(SqlHint returnHint, SqlQueryResult result, IList<ICollection<OutputColumnResult>> columnGroups, SqlStatementInfo target, IErrorReporter errorReporter)
        {
            if (result.Contracts.Count > 1 && String.IsNullOrEmpty(result.SplitOn))
            {
                errorReporter.RegisterError(target.Source, returnHint.Line, returnHint.Column, null, "The 'SplitOn' property must be specified when using multiple return types. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName> SplitOn:<SplitColumnName>");
                return;
            }

            if (columnGroups.Count > 1 && columnGroups.Any(x => !x.Any()))
            {
                errorReporter.RegisterError(target.Source, returnHint.Line, returnHint.Column, null, "Part of the 'SplitOn' property value did not match a column in the output expression");
            }

            if (columnGroups.Count != result.Contracts.Count)
            {
                errorReporter.RegisterError(target.Source, returnHint.Line, returnHint.Column, null, "The 'SplitOn' property does not match the number of return types");
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