using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class StatementOutputParser
    {
        private const string ReturnPropertyClrTypes = "ClrTypes";
        private const string ReturnPropertyMode = "Mode";
        private const string ReturnPropertyResultName = "Name";
        private const string ReturnPropertySplitOn = "SplitOn";
        private const string ReturnPropertyConverter = "Converter";
        private const string ReturnPropertyResultType = "ResultType";

        public static IEnumerable<SqlQueryResult> Parse(SqlStatementDescriptor target, TSqlFragment node, TSqlFragmentAnalyzer fragmentAnalyzer, ISqlMarkupDeclaration markup, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            StatementOutputVisitor visitor = new StatementOutputVisitor(target.Source, fragmentAnalyzer, logger);
            node.Accept(visitor);

            IList<ISqlElement> returnElements = markup.GetElements(SqlMarkupKey.Return).ToArray();
            SqlQueryResult builtInResult = GetBuiltInResult(markup, target, node, returnElements, visitor.Outputs, logger, typeResolver);

            if (builtInResult != null)
            {
                yield return builtInResult;
                yield break;
            }

            ValidateMergeGridResult(target, node, returnElements, logger);

            // Incorrect number of return elements/results will make further execution fail
            if (!ValidateReturnElements(target, returnElements, visitor.Outputs, logger))
                yield break;

            foreach (SqlQueryResult result in CollectResults(target, node, typeResolver, schemaRegistry, logger, returnElements, visitor)) 
                yield return result;
        }

        private static SqlQueryResult GetBuiltInResult(ISqlMarkupDeclaration markup, SqlStatementDescriptor target, TSqlFragment node, IEnumerable<ISqlElement> returnElements, IList<OutputSelectResult> results, ILogger logger, ITypeResolverFacade typeResolver)
        {
            SqlQueryResult fileResult = GetFileResult(markup, target, node, returnElements, results, logger, typeResolver);
            return fileResult;
        }

        private static SqlQueryResult GetFileResult(ISqlMarkupDeclaration markup, SqlStatementDescriptor target, TSqlFragment node, IEnumerable<ISqlElement> returnElements, IList<OutputSelectResult> results, ILogger logger, ITypeResolverFacade typeResolver)
        {
            bool isFileResult = markup.TryGetSingleElement(SqlMarkupKey.FileResult, target.Source, logger, out ISqlElement fileResultElement);
            if (!isFileResult)
                return null;

            ValidateFileResult(target, node, returnElements, results, logger);
            return CreateBuiltInResult("Dibix.FileEntity,Dibix", target, fileResultElement, typeResolver);
        }

        private static SqlQueryResult CreateBuiltInResult(string typeName, SqlStatementDescriptor target, ISqlElement source, ITypeResolverFacade typeResolver)
        {
            TypeReference typeReference = typeResolver.ResolveType(typeName, @namespace: null, target.Source, source.Line, source.Column, isEnumerable: false);
            return new SqlQueryResult
            {
                ResultMode = SqlQueryResultMode.SingleOrDefault,
                Types = { typeReference }
            };
        }

        private static void ValidateFileResult(SqlStatementDescriptor target, TSqlFragment node, IEnumerable<ISqlElement> returnElements, IList<OutputSelectResult> results, ILogger logger)
        {
            if (returnElements.Any())
            {
                logger.LogError(null, "When using the @FileResult option, no return declarations should be defined", target.Source, node.StartLine, node.StartColumn);
            }
            
            if (!IsValidFileResult(results))
            {
                logger.LogError(null, "When using the @FileResult option, the query should return only one output statement with the following schema: ([type] NVARCHAR, [data] VARBINARY, [filename] NVARCHAR NULL)", target.Source, node.StartLine, node.StartColumn);
            }

            if (target.MergeGridResult)
            {
                logger.LogError(null, "When using the @FileResult option, the @MergeGridResult option is invalid", target.Source, node.StartLine, node.StartColumn);
            }
        }

        private static bool IsValidFileResult(IList<OutputSelectResult> results)
        {
            if (results.Count != 1) 
                return false;

            int columnCount = results[0].Columns.Count;
            if (columnCount < 2 || columnCount > 3) 
                return false;

            ICollection<OutputColumnResult> columns = results[0].Columns;
            bool hasRequiredColumns = columns.Any(x => ValidateColumn(x, "data", SqlDataType.Binary, SqlDataType.VarBinary))
                                   && columns.Any(x => ValidateColumn(x, "type", SqlDataType.Char, SqlDataType.NChar, SqlDataType.VarChar, SqlDataType.NVarChar));

            bool isFileNameColumnValid = columnCount < 3 || columns.Any(x => ValidateColumn(x, "filename", SqlDataType.Char, SqlDataType.NChar, SqlDataType.VarChar, SqlDataType.NVarChar));

            return hasRequiredColumns && isFileNameColumnValid;
        }

        private static bool ValidateColumn(OutputColumnResult column, string expectedColumnName, params SqlDataType[] expectedColumnTypes)
        {
            if (column.ColumnName.ToLowerInvariant() != expectedColumnName)
                return false;

            if (!expectedColumnTypes.Contains(column.DataType))
                return false;

            return true;
        }

        private static void ValidateMergeGridResult(SqlStatementDescriptor target, TSqlFragment node, ICollection<ISqlElement> returnElements, ILogger logger)
        {
            if (!target.MergeGridResult || returnElements.Count > 1) 
                return;

            logger.LogError(null, "The @MergeGridResult option only works with a grid result so at least two results should be specified with the @Return hint", target.Source, node.StartLine, node.StartColumn);
        }

        private static bool ValidateReturnElements(SqlStatementDescriptor target, IList<ISqlElement> returnElements, IList<OutputSelectResult> results, ILogger logger)
        {
            bool result = true;
            for (int i = results.Count; i < returnElements.Count; i++)
            {
                ISqlElement redundantReturnElement = returnElements[i];
                logger.LogError(null, "There are more output declarations than actual outputs being produced by the statement", target.Source, redundantReturnElement.Line, redundantReturnElement.Column);
                result = false;
            }

            for (int i = returnElements.Count; i < results.Count; i++)
            {
                OutputSelectResult output = results[i];
                logger.LogError(null, "Missing return declaration for output. Please decorate the statement with the following hint to describe the output: -- @Return <ContractName>", target.Source, output.Line, output.Column);
                result = false;
            }

            return result;
        }

        private static IEnumerable<SqlQueryResult> CollectResults(SqlStatementDescriptor target, TSqlFragment node, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger, IList<ISqlElement> returnElements, StatementOutputVisitor visitor)
        {
            ICollection<string> usedOutputNames = new HashSet<string>();
            for (int i = 0; i < returnElements.Count; i++)
            {
                ISqlElement returnElement = returnElements[i];
                if (!returnElement.TryGetPropertyValue(ReturnPropertyClrTypes, isDefault: true, out ISqlElementValue typesHint))
                {
                    logger.LogError(null, $"Missing property '{ReturnPropertyClrTypes}'", target.Source, returnElement.Line, returnElement.Column);
                    yield break;
                }

                if (!TryParseResultMode(target, returnElement, logger, out SqlQueryResultMode resultMode))
                    yield break;

                ISqlElementValue resultName = returnElement.GetPropertyValue(ReturnPropertyResultName);
                ISqlElementValue converter = returnElement.GetPropertyValue(ReturnPropertyConverter);
                ISqlElementValue splitOn = returnElement.GetPropertyValue(ReturnPropertySplitOn);

                ValidateMergeGridResult(target, node, i == 0, resultMode, resultName?.Value, logger);

                IList<TypeReference> resultTypes = ParseResultTypes(target, resultMode, typesHint, typeResolver).ToArray();
                if (!resultTypes.Any())
                    continue;

                OutputSelectResult output = visitor.Outputs[i];
                ValidateResult
                (
                    isFirstResult: i == 0
                  , numberOfReturnElements: returnElements.Count
                  , returnElement: returnElement
                  , name: resultName
                  , splitOn: splitOn
                  , resultTypes: resultTypes
                  , columns: output.Columns
                  , usedOutputNames: usedOutputNames
                  , target: target
                  , schemaRegistry: schemaRegistry
                  , logger: logger
                );
                
                SqlQueryResult result = new SqlQueryResult
                {
                    Name = resultName?.Value,
                    ResultMode = resultMode,
                    Converter = converter?.Value,
                    SplitOn = splitOn?.Value,
                    ProjectToType = ParseProjectionContract(target, node, returnElements, resultMode, returnElement, typeResolver, logger)
                };
                result.Types.AddRange(resultTypes);
                result.Columns.AddRange(output.Columns.Select(x => x.ColumnName));

                yield return result;
            }
        }

        private static bool TryParseResultMode(SqlStatementDescriptor target, ISqlElement returnElement, ILogger logger, out SqlQueryResultMode resultMode)
        {
            resultMode = SqlQueryResultMode.Many;
            if (!returnElement.TryGetPropertyValue(ReturnPropertyMode, isDefault: false, out ISqlElementValue resultModeHint) || Enum.TryParse(resultModeHint.Value, out resultMode)) 
                return true;

            logger.LogError(null, $"Result mode not supported: {resultModeHint.Value}", target.Source, resultModeHint.Line, resultModeHint.Column);
            return false;
        }

        private static void ValidateMergeGridResult(SqlStatementDescriptor target, TSqlFragment node, bool isFirstResult, SqlQueryResultMode resultMode, string resultName, ILogger logger)
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

        private static IEnumerable<TypeReference> ParseResultTypes(SqlStatementDescriptor target, SqlQueryResultMode resultMode, ISqlElementValue typesHint, ITypeResolverFacade typeResolver)
        {
            string[] typeNames = typesHint.Value.Split(';');
            int column = typesHint.Column;

            foreach (string typeName in typeNames)
            {
                TypeReference typeReference = typeResolver.ResolveType(typeName, target.Namespace, target.Source, typesHint.Line, column, resultMode == SqlQueryResultMode.Many);
                if (typeReference != null)
                    yield return typeReference;

                column += typeName.Length + 1;
            }
        }

        private static void ValidateResult
        (
            bool isFirstResult
          , int numberOfReturnElements
          , ISqlElement returnElement
          , ISqlElementValue name
          , ISqlElementValue splitOn
          , IList<TypeReference> resultTypes
          , IList<OutputColumnResult> columns
          , ICollection<string> usedOutputNames
          , SqlStatementDescriptor target
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            // Name:X
            ValidateName(isFirstResult, numberOfReturnElements, returnElement, name, usedOutputNames, target, logger);
            
            // SplitOn:X
            if (!TrySplitColumns(columns, resultTypes.Count, returnElement, splitOn, target.Source, logger, out IList<ICollection<OutputColumnResult>> columnGroups))
                return;

            // Validate result columns
            for (int i = 0; i < resultTypes.Count; i++)
            {
                TypeReference returnType = resultTypes[i];
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

                    ObjectSchemaProperty property = objectSchema.Properties.SingleOrDefault(x => String.Equals(x.Name, columnResult.ColumnName, StringComparison.OrdinalIgnoreCase));

                    // Validate if entity property exists
                    if (property == null)
                    {
                        logger.LogError(null, $"Property '{columnResult.ColumnName}' not found on return type '{schema.FullName}'", target.Source, columnResult.ColumnNameSource.StartLine, columnResult.ColumnNameSource.StartColumn);
                        continue;
                    }

                    // Experimental
                    // Validate nullability
                    //if (columnResult.IsNullable.HasValue && columnResult.IsNullable.Value != property.Type.IsNullable)
                    //    logger.LogError(null, $"Nullability of column '{columnResult.ColumnName}' should match the target property", target.Source, columnResult.ColumnNameSource.StartLine, columnResult.ColumnNameSource.StartColumn);
                }
            }
        }

        private static void ValidateName(bool isFirstResult, int numberOfReturnElements, ISqlElement returnElement, ISqlElementValue name, ICollection<string> usedOutputNames, SqlStatementDescriptor target, ILogger logger)
        {
            if (name != null)
            {
                if (usedOutputNames.Contains(name.Value))
                    logger.LogError(null, $"The name '{name.Value}' is already defined for another output result", target.Source, name.Line, name.Column);
                //else if (numberOfReturnElements == 1)
                //    logger.LogError(null, "The 'Name' property is irrelevant when a single output is returned", target.Source, name.Line, name.PropertyColumn);
                else
                    usedOutputNames.Add(name.Value);
            }
            else if (numberOfReturnElements > 1 && (!target.MergeGridResult || !isFirstResult))
                logger.LogError(null, "The 'Name' property must be specified when multiple outputs are returned. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName>", target.Source, returnElement.Line, returnElement.Column);
        }

        private static bool TrySplitColumns(IList<OutputColumnResult> columns, int resultTypeCount, ISqlElement returnElement, ISqlElementValue splitOn, string source, ILogger logger, out IList<ICollection<OutputColumnResult>> columnGroups)
        {
            columnGroups = new Collection<ICollection<OutputColumnResult>>();
            if (splitOn == null)
            {
                if (resultTypeCount > 1)
                {
                    logger.LogError(null, "The 'SplitOn' property must be specified when using multiple return types. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName> SplitOn:<SplitColumnName>", source, returnElement.Line, returnElement.Column);
                    return false;
                }

                columnGroups.Add(columns);
                return true;
            }

            Queue<string> splitColumns = new Queue<string>(splitOn.Value.Split(','));
            int expectedPageCount = splitColumns.Count + 1;
            if (resultTypeCount != expectedPageCount)
            {
                logger.LogError(null, "The 'SplitOn' property does not match the number of return types", source, splitOn.Line, splitOn.Column);
                return false;
            }

            string splitColumn = null;
            int position = splitOn.Column;
            ICollection<OutputColumnResult> page = new Collection<OutputColumnResult>();
            for (int i = 0; i < columns.Count; i++)
            {
                OutputColumnResult column = columns[i];

                if (i == 1)
                {
                    splitColumn = splitColumns.Dequeue();
                }

                if (String.Equals(column.ColumnName, splitColumn, StringComparison.OrdinalIgnoreCase))
                {
                    columnGroups.Add(page);
                    page = new Collection<OutputColumnResult>();
                    if (splitColumns.Any())
                    {
                        position += 1 + splitColumn.Length;
                        splitColumn = splitColumns.Dequeue();
                    }
                    else
                        splitColumn = null;
                }

                page.Add(column);
            }

            columnGroups.Add(page);

            if (columnGroups.Count != expectedPageCount)
            {
                logger.LogError(null, $"SplitOn column '{splitColumn}' does not match any column on the result", source, splitOn.Line, position);
                return false;
            }

            return true;
        }

        private static TypeReference ParseProjectionContract(SqlStatementDescriptor target, TSqlFragment node, ICollection<ISqlElement> returnElements, SqlQueryResultMode resultMode, ISqlElement returnElement, ITypeResolverFacade typeResolver, ILogger logger)
        {
            SqlQueryResultMode[] supportedResultTypeResultModes = { SqlQueryResultMode.Many };
            if (!returnElement.TryGetPropertyValue(ReturnPropertyResultType, isDefault: false, out ISqlElementValue resultType))
                return null;

            bool singleResult = returnElements.Count <= 1;
            bool isResultTypeSupported = !supportedResultTypeResultModes.Contains(resultMode);

            if (singleResult)
            {
                // NOTE: Uncomment the Inline_SingleMultiMapResult_WithProjection test, whenever this is implemented
                // Full projection implementation attempts resulted in ambiguous runtime overloads. For example:
                // No projection: QuerySingle<TReturn, TSecond, TThird>() => Second and third are mapped to first result
                //    Projection: QuerySingle<TFirst, TSecond, TReturn>() => First and second are mapped to projected type
                // To implement this they can only be distinguished by a different method name.
                // Currently the invest is bigger than the benefit
                // The current workaround is to always have a root result (the first one) that contains at least one property (for example: the key)
                logger.LogError(null, "Projection using the 'ResultType' property is currently only supported in a part of a grid result", target.Source, node.StartLine, node.StartColumn);
            }

            if (isResultTypeSupported)
            {
                logger.LogError(null, $"Projection using the 'ResultType' property is currently only supported for the following result modes using the 'Mode' property: {String.Join(", ", supportedResultTypeResultModes)}", target.Source, node.StartLine, node.StartColumn);
            }

            if (singleResult || isResultTypeSupported) 
                return null;

            return typeResolver.ResolveType(resultType.Value, target.Namespace, target.Source, resultType.Line, resultType.Column, resultMode == SqlQueryResultMode.Many);
        }
    }
}