using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
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

        public static IEnumerable<SqlQueryResult> Parse(SqlStatementInfo target, TSqlFragment node, TSqlElementLocator elementLocator, ICollection<SqlHint> hints, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            StatementOutputVisitor visitor = new StatementOutputVisitor(target.Source, elementLocator, logger);
            node.Accept(visitor);

            IList<SqlHint> returnHints = hints.Where(x => x.Kind == SqlHint.Return).ToArray();

            ValidateFileApi(target, node, returnHints, visitor.Results, logger);
            ValidateMergeGridResult(target, node, returnHints, logger);

            // Incorrect number of return hints/results will make further execution fail
            if (!ValidateReturnHints(target, node, returnHints, visitor.Results, logger)) 
                yield break;

            foreach (SqlQueryResult result in CollectResults(target, node, typeResolver, schemaRegistry, logger, returnHints, visitor)) 
                yield return result;
        }

        private static void ValidateFileApi(SqlStatementInfo target, TSqlFragment node, IEnumerable<SqlHint> returnHints, IList<OutputSelectResult> results, ILogger logger)
        {
            if (!target.IsFileApi) 
                return;
            
            if (returnHints.Any())
            {
                logger.LogError(null, "When using the @FileApi option, no return declarations should be defined", target.Source, node.StartLine, node.StartColumn);
            }
            
            if (!IsValidFileApiResult(results))
            {
                logger.LogError(null, "When using the @FileApi option, the query should return only one output statement with the following schema: ([type] NVARCHAR, [data] VARBINARY)", target.Source, node.StartLine, node.StartColumn);
            }

            if (target.MergeGridResult)
            {
                logger.LogError(null, "When using the @FileApi option, the @MergeGridResult option is invalid", target.Source, node.StartLine, node.StartColumn);
            }
        }

        private static bool IsValidFileApiResult(IList<OutputSelectResult> results)
        {
            if (results.Count != 1) 
                return false;

            if (results[0].Columns.Count != 2) 
                return false;

            IList<OutputColumnResult> columns = results[0].Columns.OrderByDescending(x => x.ColumnName).ToArray();
            return ValidateColumn(columns[0], "type", SqlDataType.Char, SqlDataType.NChar, SqlDataType.VarChar, SqlDataType.NVarChar)
                && ValidateColumn(columns[1], "data", SqlDataType.Binary, SqlDataType.VarBinary);
        }

        private static bool ValidateColumn(OutputColumnResult column, string expectedColumnName, params SqlDataType[] expectedColumnTypes)
        {
            if (column.ColumnName.ToLowerInvariant() != expectedColumnName)
                return false;

            if (!expectedColumnTypes.Contains(column.DataTypeAccessor.Value))
                return false;

            return true;
        }

        private static void ValidateMergeGridResult(SqlStatementInfo target, TSqlFragment node, IList<SqlHint> returnHints, ILogger logger)
        {
            if (!target.MergeGridResult || returnHints.Count > 1) 
                return;

            logger.LogError(null, "The @MergeGridResult option only works with a grid result so at least two results should be specified with the @Return hint", target.Source, node.StartLine, node.StartColumn);
        }

        private static bool ValidateReturnHints(SqlStatementInfo target, TSqlFragment node, IList<SqlHint> returnHints, ICollection<OutputSelectResult> results, ILogger logger)
        {
            if (target.IsFileApi)
                return true;

            if (returnHints.Count > results.Count)
            {
                logger.LogError(null, "There are more return declarations than output statements being produced by the statement", target.Source, node.StartLine, node.StartColumn);
                return false;
            }

            if (returnHints.Count < results.Count)
            {
                logger.LogError(null, "There are missing return declarations for the output statements. Please mark the header of the statement with a line per output containting this hint: -- @Return <ClrTypeName>", target.Source, node.StartLine, node.StartColumn);
                return false;
            }

            return true;
        }

        private static IEnumerable<SqlQueryResult> CollectResults(SqlStatementInfo target, TSqlFragment node, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger, IList<SqlHint> returnHints, StatementOutputVisitor visitor)
        {
            ICollection<string> usedOutputNames = new HashSet<string>();
            for (int i = 0; i < returnHints.Count; i++)
            {
                SqlHint returnHint = returnHints[i];
                if (!returnHint.TrySelectValueOrContent(ReturnHintClrTypes, x => logger.LogError(null, x, target.Source, node.StartLine, node.StartColumn), out string typeNamesStr))
                    yield break;

                if (!TryParseResultMode(target, node, returnHint, logger, out SqlQueryResultMode resultMode))
                    yield break;

                string resultName = returnHint.SelectValueOrDefault(ReturnHintResultName);
                string converter = returnHint.SelectValueOrDefault(ReturnHintConverter);
                string splitOn = returnHint.SelectValueOrDefault(ReturnHintSplitOn);

                ValidateMergeGridResult(target, node, i == 0, resultMode, resultName, logger);

                SqlQueryResult result = new SqlQueryResult
                {
                    Name = resultName,
                    ResultMode = resultMode,
                    Converter = converter,
                    SplitOn = splitOn,
                    ProjectToType = ParseProjectionContract(target, node, returnHints, resultMode, returnHint, typeResolver, logger)
                };

                if (!TryParseResultTypes(target, resultMode, returnHint, typeNamesStr, typeResolver, out IEnumerable<TypeReference> types))
                    continue;

                result.Types.AddRange(types);

                OutputSelectResult output = visitor.Results[i];
                ValidateResult(i == 0, returnHints.Count, returnHint, result, output.Columns, usedOutputNames, target, schemaRegistry, logger);
                result.Columns.AddRange(output.Columns.Select(x => x.ColumnName));

                yield return result;
            }
        }

        private static bool TryParseResultMode(SqlStatementInfo target, TSqlFragment node, SqlHint returnHint, ILogger logger, out SqlQueryResultMode resultMode)
        {
            string resultModeStr = returnHint.SelectValueOrDefault(ReturnHintMode);
            resultMode = SqlQueryResultMode.Many;
            if (resultModeStr == null || Enum.TryParse(resultModeStr, out resultMode)) 
                return true;

            logger.LogError(null, $"Result mode not supported: {resultModeStr}", target.Source, node.StartLine, node.StartColumn);
            return false;
        }

        private static void ValidateMergeGridResult(SqlStatementInfo target, TSqlFragment node, bool isFirstResult, SqlQueryResultMode resultMode, string resultName, ILogger logger)
        {
            SqlQueryResultMode[] supportedMergeGridResultModes = { SqlQueryResultMode.Single, SqlQueryResultMode.SingleOrDefault };
            if (!target.MergeGridResult || !isFirstResult) 
                return;

            if (!supportedMergeGridResultModes.Contains(resultMode))
            {
                logger.LogError(null, $"When using the @MergeGridResult option, the first result should specify one of the following result modes using the 'Mode' property: {String.Join(", ", supportedMergeGridResultModes)}", target.Source, node.StartLine, node.StartColumn);
            }

            if (!String.IsNullOrEmpty(resultName))
            {
                logger.LogError(null, "When using the @MergeGridResult option, the 'Name' property must not be set on the first result", target.Source, node.StartLine, node.StartColumn);
            }
        }

        private static TypeReference ParseProjectionContract(SqlStatementInfo target, TSqlFragment node, ICollection<SqlHint> returnHints, SqlQueryResultMode resultMode, SqlHint returnHint, ITypeResolverFacade typeResolver, ILogger logger)
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
                logger.LogError(null, "Projection using the 'ResultType' property is currently only supported in a part of a grid result", target.Source, node.StartLine, node.StartColumn);
            }

            if (isResultTypeSupported)
            {
                logger.LogError(null, $"Projection using the 'ResultType' property is currently only supported for the following result modes using the 'Mode' property: {String.Join(", ", supportedResultTypeResultModes)}", target.Source, node.StartLine, node.StartColumn);
            }

            if (singleResult || isResultTypeSupported) 
                return null;

            return typeResolver.ResolveType(resultTypeStr, target.Namespace, target.Source, returnHint.Line, returnHint.Column, resultMode == SqlQueryResultMode.Many);
        }

        private static bool TryParseResultTypes(SqlStatementInfo target, SqlQueryResultMode resultMode, SqlHint returnHint, string typeNamesStr, ITypeResolverFacade typeResolver, out IEnumerable<TypeReference> types)
        {
            string[] typeNames = typeNamesStr.Split(';');
            IList<TypeReference> returnTypes = typeNames.Select(x => typeResolver.ResolveType(x, target.Namespace, target.Source, returnHint.Line, returnHint.Column, resultMode == SqlQueryResultMode.Many)).ToArray();
            if (returnTypes.All(x => x != null))
            {
                types = returnTypes;
                return true;
            }

            types = Enumerable.Empty<TypeReference>();
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
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            ValidateName(isFirstResult, numberOfReturnHints, returnHint, result, usedOutputNames, target, logger);

            IList<ICollection<OutputColumnResult>> columnGroups = SplitColumns(columns, result.SplitOn).ToArray();
            ValidateSplitOn(returnHint, result, columnGroups, target, logger);

            for (int i = 0; i < result.Types.Count; i++)
            {
                TypeReference returnType = result.Types[i];
                ICollection<OutputColumnResult> columnGroup = columnGroups[i];
                SchemaDefinition schema = null;
                if (returnType is SchemaTypeReference schemaTypeReference)
                    schema = schemaRegistry.GetSchema(schemaTypeReference);

                // SELECT 1 => Query<int>/Single<int> => No entity property validation + No missing alias validation
                bool singleColumn = columnGroup.Count == 1;
                bool isPrimitive = returnType is PrimitiveTypeReference || schema is EnumSchema;
                if (singleColumn && isPrimitive)
                    continue;

                if (schema == null)
                    continue;

                if (!(schema is ObjectSchema objectSchema))
                    throw new NotSupportedException($"Unsupported return type for result validation: {returnType.GetType()}");

                foreach (OutputColumnResult columnResult in columnGroup)
                {
                    // Validate alias
                    // i.E.: SELECT COUNT(*) no alias
                    if (!columnResult.HasName)
                    {
                        logger.LogError(null, $"Missing alias for expression '{columnResult.PrimarySource.Dump()}'", target.Source, columnResult.PrimarySource.StartLine, columnResult.PrimarySource.StartColumn);
                        continue;
                    }

                    // Validate if entity property exists
                    if (objectSchema.Properties.All(x => !String.Equals(x.Name, columnResult.ColumnName, StringComparison.OrdinalIgnoreCase)))
                        logger.LogError(null, $"Property '{columnResult.ColumnName}' not found on return type '{schema.FullName}'", target.Source, columnResult.ColumnNameSource.StartLine, columnResult.ColumnNameSource.StartColumn);
                }
            }
        }

        private static void ValidateName(bool isFirstResult, int numberOfReturnHints, SqlHint returnHint, SqlQueryResult result, ICollection<string> usedOutputNames, SqlStatementInfo target, ILogger logger)
        {
            if (!String.IsNullOrEmpty(result.Name))
            {
                if (usedOutputNames.Contains(result.Name))
                    logger.LogError(null, $"The name '{result.Name}' is already defined for another output result", target.Source, returnHint.Line, returnHint.Column);
                else if (numberOfReturnHints == 1)
                    logger.LogError(null, "The 'Name' property is irrelevant when a single output is returned", target.Source, returnHint.Line, returnHint.Column);
                else
                    usedOutputNames.Add(result.Name);
            }
            else if (numberOfReturnHints > 1 && (!target.MergeGridResult || !isFirstResult))
                logger.LogError(null, "The 'Name' property must be specified when multiple outputs are returned. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName>", target.Source, returnHint.Line, returnHint.Column);
        }

        private static void ValidateSplitOn(SqlHint returnHint, SqlQueryResult result, IList<ICollection<OutputColumnResult>> columnGroups, SqlStatementInfo target, ILogger logger)
        {
            if (result.Types.Count > 1 && String.IsNullOrEmpty(result.SplitOn))
            {
                logger.LogError(null, "The 'SplitOn' property must be specified when using multiple return types. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName> SplitOn:<SplitColumnName>", target.Source, returnHint.Line, returnHint.Column);
                return;
            }

            if (columnGroups.Count > 1 && columnGroups.Any(x => !x.Any()))
            {
                logger.LogError(null, "Part of the 'SplitOn' property value did not match a column in the output expression", target.Source, returnHint.Line, returnHint.Column);
            }

            if (columnGroups.Count != result.Types.Count)
            {
                logger.LogError(null, "The 'SplitOn' property does not match the number of return types", target.Source, returnHint.Line, returnHint.Column);
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