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

        public static IEnumerable<SqlQueryResult> Parse(SqlStatementDefinition definition, TSqlFragment node, string source, TSqlFragmentAnalyzer fragmentAnalyzer, ISqlMarkupDeclaration markup, string relativeNamespace, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            StatementOutputVisitor visitor = new StatementOutputVisitor(source, fragmentAnalyzer, logger);
            node.Accept(visitor);

            IList<ISqlElement> returnElements = markup.GetElements(SqlMarkupKey.Return).ToArray();
            SqlQueryResult builtInResult = GetBuiltInResult(markup, definition, node, source, returnElements, visitor.Outputs, logger, typeResolver);

            if (builtInResult != null)
            {
                yield return builtInResult;
                yield break;
            }

            ValidateMergeGridResult(definition, node, source, returnElements, logger);

            // Incorrect number of return elements/results will make further execution fail
            if (!ValidateReturnElements(source, returnElements, visitor.Outputs, logger))
                yield break;

            foreach (SqlQueryResult result in CollectResults(definition, node, typeResolver, schemaRegistry, logger, returnElements, visitor, relativeNamespace)) 
                yield return result;
        }

        private static SqlQueryResult GetBuiltInResult(ISqlMarkupDeclaration markup, SqlStatementDefinition definition, TSqlFragment node, string source, IEnumerable<ISqlElement> returnElements, IList<OutputSelectResult> results, ILogger logger, ITypeResolverFacade typeResolver)
        {
            SqlQueryResult fileResult = GetFileResult(markup, definition, node, source, returnElements, results, logger, typeResolver);
            return fileResult;
        }

        private static SqlQueryResult GetFileResult(ISqlMarkupDeclaration markup, SqlStatementDefinition definition, TSqlFragment node, string source, IEnumerable<ISqlElement> returnElements, IList<OutputSelectResult> results, ILogger logger, ITypeResolverFacade typeResolver)
        {
            bool isFileResult = markup.TryGetSingleElement(SqlMarkupKey.FileResult, source, logger, out ISqlElement fileResultElement);
            if (!isFileResult)
                return null;

            ValidateFileResult(definition, node, source, returnElements, results, logger);
            return CreateBuiltInResult("Dibix.FileEntity,Dibix", fileResultElement, typeResolver);
        }

        private static SqlQueryResult CreateBuiltInResult(string typeName, ISqlElement element, ITypeResolverFacade typeResolver)
        {
            TypeReference typeReference = typeResolver.ResolveType(typeName, relativeNamespace: null, element.Source, element.Line, element.Column, isEnumerable: false);
            return new SqlQueryResult
            {
                ResultMode = SqlQueryResultMode.SingleOrDefault,
                Types = { typeReference },
                ReturnType = typeReference
            };
        }

        private static void ValidateFileResult(SqlStatementDefinition definition, TSqlFragment node, string source, IEnumerable<ISqlElement> returnElements, IList<OutputSelectResult> results, ILogger logger)
        {
            if (returnElements.Any())
            {
                logger.LogError(null, "When using the @FileResult option, no return declarations should be defined", source, node.StartLine, node.StartColumn);
            }
            
            if (!IsValidFileResult(results))
            {
                logger.LogError(null, "When using the @FileResult option, the query should return only one output statement with the following schema: ([type] NVARCHAR, [data] VARBINARY, [filename] NVARCHAR NULL)", source, node.StartLine, node.StartColumn);
            }

            if (definition.MergeGridResult)
            {
                logger.LogError(null, "When using the @FileResult option, the @MergeGridResult option is invalid", source, node.StartLine, node.StartColumn);
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

        private static void ValidateMergeGridResult(SqlStatementDefinition definition, TSqlFragment node, string source, ICollection<ISqlElement> returnElements, ILogger logger)
        {
            if (!definition.MergeGridResult || returnElements.Count > 1) 
                return;

            logger.LogError(null, "The @MergeGridResult option only works with a grid result so at least two results should be specified with the @Return hint", source, node.StartLine, node.StartColumn);
        }

        private static bool ValidateReturnElements(string source, IList<ISqlElement> returnElements, IList<OutputSelectResult> results, ILogger logger)
        {
            bool result = true;
            for (int i = results.Count; i < returnElements.Count; i++)
            {
                ISqlElement redundantReturnElement = returnElements[i];
                logger.LogError(null, "There are more output declarations than actual outputs being produced by the statement", source, redundantReturnElement.Line, redundantReturnElement.Column);
                result = false;
            }

            for (int i = returnElements.Count; i < results.Count; i++)
            {
                OutputSelectResult output = results[i];
                logger.LogError(null, "Missing return declaration for output. Please decorate the statement with the following hint to describe the output: -- @Return <ContractName>", source, output.Line, output.Column);
                result = false;
            }

            return result;
        }

        private static IEnumerable<SqlQueryResult> CollectResults(SqlStatementDefinition definition, TSqlFragment node, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger, IList<ISqlElement> returnElements, StatementOutputVisitorBase visitor, string relativeNamespace)
        {
            ICollection<string> usedOutputNames = new HashSet<string>();
            for (int i = 0; i < returnElements.Count; i++)
            {
                ISqlElement returnElement = returnElements[i];
                if (!returnElement.TryGetPropertyValue(ReturnPropertyClrTypes, isDefault: true, out ISqlElementValue typesHint))
                {
                    logger.LogError(null, $"Missing property '{ReturnPropertyClrTypes}'", returnElement.Source, returnElement.Line, returnElement.Column);
                    yield break;
                }

                if (!TryParseResultMode(returnElement, logger, out SqlQueryResultMode resultMode))
                    yield break;

                ISqlElementValue resultName = returnElement.GetPropertyValue(ReturnPropertyResultName);
                ISqlElementValue converter = returnElement.GetPropertyValue(ReturnPropertyConverter);
                ISqlElementValue splitOn = returnElement.GetPropertyValue(ReturnPropertySplitOn);

                ValidateMergeGridResult(definition, node, returnElement.Source, i == 0, resultMode, resultName?.Value, logger);
                TypeReference projectToType = ParseProjectionContract(node, returnElements, resultMode, returnElement, relativeNamespace, typeResolver, logger, out ISqlElementValue projectToTypeElement);

                string[] typeNames = typesHint.Value.Split(';');
                IList<TypeReference> resultTypes = ParseResultTypes(typeNames, returnElement.Source, resultMode, typesHint, typeResolver, relativeNamespace).ToArray();
                if (!resultTypes.Any())
                    continue;

                TypeReference returnType;
                string returnTypeName;
                ISqlElementValue returnTypeLocation;
                if (projectToType != null)
                {
                    returnType = projectToType;
                    returnTypeName = projectToTypeElement.Value;
                    returnTypeLocation = projectToTypeElement;
                }
                else
                {
                    returnType = resultTypes[0];
                    returnTypeName = typeNames[0];
                    returnTypeLocation = typesHint;
                }

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
                  , definition: definition
                  , schemaRegistry: schemaRegistry
                  , logger: logger
                );
                
                SqlQueryResult result = new SqlQueryResult
                {
                    Name = resultName?.Value,
                    ResultMode = resultMode,
                    Converter = converter?.Value,
                    SplitOn = splitOn?.Value,
                    ProjectToType = projectToType,
                    ReturnType = returnType
                };
                result.Types.AddRange(resultTypes);
                result.Columns.AddRange(output.Columns.Select(x => x.ColumnName));

                if (!ValidateReturnType(returnType, returnTypeName, returnElement.Source, returnTypeLocation, resultMode, logger))
                    continue;

                yield return result;
            }
        }

        private static bool TryParseResultMode(ISqlElement returnElement, ILogger logger, out SqlQueryResultMode resultMode)
        {
            resultMode = SqlQueryResultMode.Many;
            if (!returnElement.TryGetPropertyValue(ReturnPropertyMode, isDefault: false, out ISqlElementValue resultModeHint) || Enum.TryParse(resultModeHint.Value, out resultMode)) 
                return true;

            logger.LogError(null, $"Result mode not supported: {resultModeHint.Value}", returnElement.Source, resultModeHint.Line, resultModeHint.Column);
            return false;
        }

        private static void ValidateMergeGridResult(SqlStatementDefinition definition, TSqlFragment node, string source, bool isFirstResult, SqlQueryResultMode resultMode, string resultName, ILogger logger)
        {
            SqlQueryResultMode[] supportedMergeGridResultModes = { SqlQueryResultMode.Single, SqlQueryResultMode.SingleOrDefault };
            if (!definition.MergeGridResult || !isFirstResult)
                return;

            if (!supportedMergeGridResultModes.Contains(resultMode))
            {
                logger.LogError(null, $"When using the @MergeGridResult option, the first result should specify one of the following result modes using the 'Mode' property: {String.Join(", ", supportedMergeGridResultModes)}", source, node.StartLine, node.StartColumn);
            }

            if (!String.IsNullOrEmpty(resultName))
            {
                logger.LogError(null, "When using the @MergeGridResult option, the 'Name' property must not be set on the first result", source, node.StartLine, node.StartColumn);
            }
        }

        private static IEnumerable<TypeReference> ParseResultTypes(IEnumerable<string> typeNames, string source, SqlQueryResultMode resultMode, ISqlElementValue typesHint, ITypeResolverFacade typeResolver, string relativeNamespace)
        {
            int column = typesHint.Column;

            foreach (string typeName in typeNames)
            {
                TypeReference typeReference = typeResolver.ResolveType(typeName, relativeNamespace, source, typesHint.Line, column, resultMode == SqlQueryResultMode.Many);
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
          , SqlStatementDefinition definition
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        )
        {
            // Name:X
            ValidateName(isFirstResult, numberOfReturnElements, returnElement, name, usedOutputNames, definition, logger);
            
            // SplitOn:X
            if (!TrySplitColumns(columns, resultTypes.Count, returnElement, splitOn, logger, out IList<IList<OutputColumnResult>> columnGroups))
                return;

            // Validate result columns
            for (int i = 0; i < resultTypes.Count; i++)
            {
                TypeReference returnType = resultTypes[i];
                IList<OutputColumnResult> columnGroup = columnGroups[i];

                if (!columnGroup.Any())
                    throw new InvalidOperationException("Result does not contain any columns");

                SchemaDefinition schema = null;
                if (returnType is SchemaTypeReference schemaTypeReference)
                    schema = schemaRegistry.GetSchema(schemaTypeReference);
                
                string primitiveTypeName;
                
                if (returnType is PrimitiveTypeReference primitiveTypeReference)
                    primitiveTypeName = primitiveTypeReference.Type.ToString();
                else if (schema is EnumSchema enumSchema)
                    primitiveTypeName = enumSchema.FullName;
                else
                    primitiveTypeName = null;

                if (primitiveTypeName != null)
                {
                    bool singleColumn = columnGroup.Count == 1;
                    if (!singleColumn)
                    {
                        TSqlFragment firstColumn = columnGroup[0].ColumnNameSource;
                        logger.LogError(null, $"Cannot map complex result to primitive type '{primitiveTypeName}'", returnElement.Source, firstColumn.StartLine, firstColumn.StartColumn);
                    }

                    // SELECT 1 => Query<int>/Single<int> => No entity property validation + No missing alias validation
                    continue;
                }

                if (!(schema is ObjectSchema objectSchema))
                    throw new NotSupportedException($"Unsupported return type for result validation: {returnType.GetType()}");

                foreach (OutputColumnResult columnResult in columnGroup)
                {
                    // Validate alias
                    // i.E.: SELECT COUNT(*) no alias
                    if (!columnResult.HasName)
                    {
                        logger.LogError(null, $"Missing alias for expression '{columnResult.PrimarySource.Dump()}'", returnElement.Source, columnResult.PrimarySource.StartLine, columnResult.PrimarySource.StartColumn);
                        continue;
                    }

                    ObjectSchemaProperty property = objectSchema.Properties.SingleOrDefault(x => String.Equals(x.Name, columnResult.ColumnName, StringComparison.OrdinalIgnoreCase));

                    // Validate if entity property exists
                    if (property == null)
                    {
                        logger.LogError(null, $"Property '{columnResult.ColumnName}' not found on return type '{schema.FullName}'", returnElement.Source, columnResult.ColumnNameSource.StartLine, columnResult.ColumnNameSource.StartColumn);
                        continue;
                    }

                    // Experimental
                    // Validate nullability
                    //if (columnResult.IsNullable.HasValue && columnResult.IsNullable.Value != property.Type.IsNullable)
                    //    logger.LogError(null, $"Nullability of column '{columnResult.ColumnName}' should match the target property", target.Source, columnResult.ColumnNameSource.StartLine, columnResult.ColumnNameSource.StartColumn);
                }
            }
        }

        private static void ValidateName(bool isFirstResult, int numberOfReturnElements, ISqlElement returnElement, ISqlElementValue name, ICollection<string> usedOutputNames, SqlStatementDefinition target, ILogger logger)
        {
            if (name != null)
            {
                if (usedOutputNames.Contains(name.Value))
                    logger.LogError(null, $"The name '{name.Value}' is already defined for another output result", returnElement.Source, name.Line, name.Column);
                //else if (numberOfReturnElements == 1)
                //    logger.LogError(null, "The 'Name' property is irrelevant when a single output is returned", target.Source, name.Line, name.PropertyColumn);
                else
                    usedOutputNames.Add(name.Value);
            }
            else if (numberOfReturnElements > 1 && (!target.MergeGridResult || !isFirstResult))
                logger.LogError(null, "The 'Name' property must be specified when multiple outputs are returned. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName>", returnElement.Source, returnElement.Line, returnElement.Column);
        }

        private static bool TrySplitColumns(IList<OutputColumnResult> columns, int resultTypeCount, ISqlElement returnElement, ISqlElementValue splitOn, ILogger logger, out IList<IList<OutputColumnResult>> columnGroups)
        {
            columnGroups = new Collection<IList<OutputColumnResult>>();
            if (splitOn == null)
            {
                if (resultTypeCount > 1)
                {
                    logger.LogError(null, "The 'SplitOn' property must be specified when using multiple return types. Mark it in the @Return hint: -- @Return ClrTypes:<ClrTypeName> Name:<ResultName> SplitOn:<SplitColumnName>", returnElement.Source, returnElement.Line, returnElement.Column);
                    return false;
                }

                columnGroups.Add(columns);
                return true;
            }

            Queue<string> splitColumns = new Queue<string>(splitOn.Value.Split(','));
            int expectedPageCount = splitColumns.Count + 1;
            if (resultTypeCount != expectedPageCount)
            {
                logger.LogError(null, "The 'SplitOn' property does not match the number of return types", returnElement.Source, splitOn.Line, splitOn.Column);
                return false;
            }

            string splitColumn = null;
            int position = splitOn.Column;
            IList<OutputColumnResult> page = new Collection<OutputColumnResult>();
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
                logger.LogError(null, $"SplitOn column '{splitColumn}' does not match any column on the result", returnElement.Source, splitOn.Line, position);
                return false;
            }

            return true;
        }

        private static TypeReference ParseProjectionContract(TSqlFragment node, ICollection<ISqlElement> returnElements, SqlQueryResultMode resultMode, ISqlElement returnElement, string relativeNamespace, ITypeResolverFacade typeResolver, ILogger logger, out ISqlElementValue resultType)
        {
            SqlQueryResultMode[] supportedResultTypeResultModes = { SqlQueryResultMode.Many };
            if (!returnElement.TryGetPropertyValue(ReturnPropertyResultType, isDefault: false, out resultType))
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
                logger.LogError(null, "Projection using the 'ResultType' property is currently only supported in a part of a grid result", returnElement.Source, node.StartLine, node.StartColumn);
            }

            if (isResultTypeSupported)
            {
                logger.LogError(null, $"Projection using the 'ResultType' property is currently only supported for the following result modes using the 'Mode' property: {String.Join(", ", supportedResultTypeResultModes)}", returnElement.Source, node.StartLine, node.StartColumn);
            }

            if (singleResult || isResultTypeSupported) 
                return null;

            return typeResolver.ResolveType(resultType.Value, relativeNamespace, returnElement.Source, resultType.Line, resultType.Column, resultMode == SqlQueryResultMode.Many);
        }

        private static bool ValidateReturnType(TypeReference returnType, string returnTypeName, string source, ISqlElementValue returnTypeLocation, SqlQueryResultMode mode, ILogger logger)
        {
            // The point of this rule, is to stability the meaning of 'Default'.
            // In C# all value types have a default, but they're not all the same (0, false, 0.0, "" for nullable-ref-types feature)
            // This rule tries to isolate the default to two values: null and false (boolean)
            if (mode != SqlQueryResultMode.SingleOrDefault)
                return true;

            if (returnType.IsNullable)
                return true;

            if (!(returnType is PrimitiveTypeReference primitiveTypeReference))
                return true;

            // In case of a boolean result, there are two options to fix the violation:
            // 1. Change mode to 'Single' and make the statement always one row.
            //    => This introduces complexity and decreases readability by wrapping the statement into an IIF statement
            //    => It might even have a slight impact on performance to always return a row, even if it's not needed.
            // 2. Return a nullable boolean
            //    => This is bad design and inconvenient for the API consumer, as he associates words like 'Can', 'Allow', etc. with a true boolean answer (yes/no).
            //    => The caller does not know what NULL in this case actually means.
            // Regarding these downsides, having boolean results violate this rule, is inconvenient, therefore we allow it.
            if (primitiveTypeReference.Type == PrimitiveType.Boolean)
                return true;

            logger.LogError(null, $"When using the result mode option '{mode}', the primitive return type must be nullable: {returnTypeName}", source, returnTypeLocation.Line, returnTypeLocation.Column);
            return false;
        }
    }
}