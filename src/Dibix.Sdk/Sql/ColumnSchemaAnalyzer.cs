using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.Tools.Schema.Extensibility;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Assembly = System.Reflection.Assembly;

namespace Dibix.Sdk.Sql
{
    internal static class ColumnSchemaAnalyzer
    {
        private static readonly Assembly DacExtensionsAssembly = typeof(Microsoft.SqlServer.Dac.Model.Assembly).Assembly;
        private static readonly Assembly SchemaSqlAssembly = typeof(IExtension).Assembly;
        private static readonly Type TSqlModelType = typeof(TSqlModel);
        private static readonly Type TSqlObjectType = typeof(TSqlObject);
        private static readonly Type SchemaAnalysisServiceType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.SchemaAnalysisService", true);
        private static readonly Lazy<SqlAnalysisRule, ISqlAnalysisRuleMetadata> LazyExport = new Lazy<SqlAnalysisRule, ISqlAnalysisRuleMetadata>(() => null, new ExportCodeAnalysisRuleAttribute("0", null));
        private static readonly AnalyzeColumns ColumnAnalyzer = CompileColumnAnalyzer();

        public static ColumnSchemaAnalyzerResult Analyze(TSqlModel dataSchemaModel, TSqlFragment sqlFragment)
        {
            TSqlObject modelElement = dataSchemaModel.GetObject(ModelSchema.User, new ObjectIdentifier("sys"), DacQueryScopes.BuiltIn);
            return ColumnAnalyzer(dataSchemaModel, modelElement, sqlFragment);
        }

        private static AnalyzeColumns CompileColumnAnalyzer()
        {
            ParameterExpression dataSchemaModelParameter = Expression.Parameter(TSqlModelType, "dataSchemaModel");
            ParameterExpression modelElementParameter = Expression.Parameter(TSqlObjectType, "modelElement");
            ParameterExpression sqlFragmentParameter = Expression.Parameter(typeof(TSqlFragment), "sqlFragment");

            ColumnSchemaAnalyzerCompilationContext context = new ColumnSchemaAnalyzerCompilationContext();

            // ColumnSchemaAnalyzerResult result = new ColumnSchemaAnalyzerResult();
            Type columnSchemaAnalyzerResultType = typeof(ColumnSchemaAnalyzerResult);
            ParameterExpression resultVariable = Expression.Variable(columnSchemaAnalyzerResultType, "result");
            Expression resultValue = Expression.New(columnSchemaAnalyzerResultType);
            Expression resultAssign = Expression.Assign(resultVariable, resultValue);
            context.Variables.Add(resultVariable);
            context.Statements.Add(resultAssign);

            CompileNullableColumnSchemaAnalyzerResults(context, dataSchemaModelParameter, modelElementParameter, sqlFragmentParameter, resultVariable);
            CompileSqlInterpretationVisitor90Results(context, dataSchemaModelParameter, sqlFragmentParameter, resultVariable);

            context.Statements.Add(resultVariable);
            Expression block = Expression.Block(context.Variables, context.Statements);
            Expression<AnalyzeColumns> lambda = Expression.Lambda<AnalyzeColumns>
            (
                block
              , dataSchemaModelParameter
              , modelElementParameter
              , sqlFragmentParameter
            );
            AnalyzeColumns compiled = lambda.Compile();
            return compiled;
        }

        private static void CompileNullableColumnSchemaAnalyzerResults(ColumnSchemaAnalyzerCompilationContext context, Expression dataSchemaModelParameter, Expression modelElementParameter, Expression sqlFragmentParameter, Expression resultVariable)
        {
            // ExtensionDescriptor<SqlAnalysisRule, ISqlAnalysisRuleMetadata> extensionDescriptor = new ExtensionDescriptor<SqlAnalysisRule, ISqlAnalysisRuleMetadata>(ColumnAnalyzer.LazyExport);
            Expression lazyExport = Expression.Field(null, typeof(ColumnSchemaAnalyzer), nameof(LazyExport));
            Type extensionDescriptorType = SchemaSqlAssembly.GetType("Microsoft.SqlServer.Dac.Extensibility.ExtensionDescriptor`2", true)
                                                            .MakeGenericType(typeof(SqlAnalysisRule), typeof(ISqlAnalysisRuleMetadata));
            ConstructorInfo extensionDescriptorCtor = extensionDescriptorType.GetConstructor(new[] { lazyExport.Type });
            Guard.IsNotNull(extensionDescriptorCtor, nameof(extensionDescriptorCtor), "Could not find constructor on ExtensionDescriptor<,>");
            ParameterExpression extensionDescriptorVariable = Expression.Variable(extensionDescriptorType, "extensionDescriptor");
            Expression extensionDescriptorValue = Expression.New(extensionDescriptorCtor, lazyExport);
            Expression extensionDescriptorAssign = Expression.Assign(extensionDescriptorVariable, extensionDescriptorValue);
            context.Variables.Add(extensionDescriptorVariable);
            context.Statements.Add(extensionDescriptorAssign);

            // RuleDescriptor ruleDescriptor = new RuleDescriptorImpl(extensionDescriptor);
            Type ruleDescriptorType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.RuleDescriptor", true);
            Type ruleDescriptorImplType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.Engine.RuleDescriptorImpl", true);
            ConstructorInfo ruleDescriptorImplCtor = ruleDescriptorImplType.GetConstructor(new[] { extensionDescriptorType });
            Guard.IsNotNull(ruleDescriptorImplCtor, nameof(ruleDescriptorImplCtor), "Could not find constructor on RuleDescriptorImpl");
            ParameterExpression ruleDescriptorVariable = Expression.Variable(ruleDescriptorType, "ruleDescriptor");
            Expression ruleDescriptorValue = Expression.New(ruleDescriptorImplCtor, extensionDescriptorVariable);
            Expression ruleDescriptorAssign = Expression.Assign(ruleDescriptorVariable, ruleDescriptorValue);
            context.Variables.Add(ruleDescriptorVariable);
            context.Statements.Add(ruleDescriptorAssign);

            // NullableColumnSchemaAnalyzer nullableColumnSchemaAnalyzer = new NullableColumnSchemaAnalyzer(ruleDescriptor, dataSchemaModel, modelElement);
            Type nullableColumnSchemaAnalyzerType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.Rules.Performance.NullableColumnSchemaAnalyzer");
            ConstructorInfo nullableColumnSchemaAnalyzerCtor = nullableColumnSchemaAnalyzerType.GetConstructor(new[] { ruleDescriptorType, TSqlModelType, TSqlObjectType });
            Guard.IsNotNull(nullableColumnSchemaAnalyzerCtor, nameof(nullableColumnSchemaAnalyzerCtor), "Could not find constructor on NullableColumnSchemaAnalyzer");
            ParameterExpression nullableColumnSchemaAnalyzerVariable = Expression.Variable(nullableColumnSchemaAnalyzerType, "nullableColumnSchemaAnalyzer");
            Expression nullableColumnSchemaAnalyzerValue = Expression.New(nullableColumnSchemaAnalyzerCtor, ruleDescriptorVariable, dataSchemaModelParameter, modelElementParameter);
            Expression nullableColumnSchemaAnalyzerAssign = Expression.Assign(nullableColumnSchemaAnalyzerVariable, nullableColumnSchemaAnalyzerValue);
            context.Variables.Add(nullableColumnSchemaAnalyzerVariable);
            context.Statements.Add(nullableColumnSchemaAnalyzerAssign);
            context.Analyzer = nullableColumnSchemaAnalyzerVariable;

            // SchemaAnalysisService schemaAnalysisService = SchemaAnalysisService.CreateAnalysisService(nullableColumnSchemaAnalyzer);
            ParameterExpression schemaAnalysisServiceVariable = Expression.Variable(SchemaAnalysisServiceType, "schemaAnalysisService");
            Expression schemaAnalysisServiceValue = Expression.Call(SchemaAnalysisServiceType, "CreateAnalysisService", new Type[0], nullableColumnSchemaAnalyzerVariable);
            Expression schemaAnalysisServiceAssign = Expression.Assign(schemaAnalysisServiceVariable, schemaAnalysisServiceValue);
            context.Variables.Add(schemaAnalysisServiceVariable);
            context.Statements.Add(schemaAnalysisServiceAssign);
            context.SchemaAnalysisService = schemaAnalysisServiceVariable;

            // IList<ExtensibilityError> interpretationErrors = schemaAnalysisService.AnalyzeScript(model, sqlFragment);
            ParameterExpression interpretationErrorsVariable = Expression.Variable(typeof(IList<ExtensibilityError>), "interpretationErrors");
            Expression interpretationErrorsValue = Expression.Call(schemaAnalysisServiceVariable, "AnalyzeScript", new Type[0], dataSchemaModelParameter, sqlFragmentParameter);
            Expression interpretationErrorsAssign = Expression.Assign(interpretationErrorsVariable, interpretationErrorsValue);
            context.Variables.Add(interpretationErrorsVariable);
            context.Statements.Add(interpretationErrorsAssign);

            // result.Errors.AddRange(interpretationErrors);
            Expression errorsProperty = Expression.Property(resultVariable, nameof(ColumnSchemaAnalyzerResult.Errors));
            Expression interpretationErrorsAddRange = Expression.Call(typeof(CollectionExtensions), nameof(CollectionExtensions.AddRange), new[] { typeof(ExtensibilityError) }, errorsProperty, interpretationErrorsVariable);
            context.Statements.Add(interpretationErrorsAddRange);

            // IEnumerator<KeyValuePair<int, ElementDescriptor>> columnOffsetDescriptorEnumerator;
            // try
            // {
            //     columnOffsetDescriptorEnumerator = nullableColumnSchemaAnalyzer._columnOffsetToDescriptorMap.GetEnumerator();
            //     while (columnOffsetDescriptorEnumerator.MoveNext())
            //     {
            //         KeyValuePair<int, ElementDescriptor> columnOffsetDescriptor = columnOffsetDescriptorEnumerator.Current;
            //         int offset = columnOffsetDescriptor.Key;
            //         TSqlObject columnElement = columnOffsetDescriptor.Value.GetModelElement(dataSchemaModel);
            //         ColumnElementDescriptor column = new ColumnElementDescriptor(offset, columnElement);
            //         columns.Add(column);
            //     }
            // }
            // finally
            // {
            //     columnOffsetDescriptorEnumerator.Dispose();
            // }
            CompileNullableColumnSchemaAnalyzerResultsIterator(context, dataSchemaModelParameter, resultVariable);
        }

        private static void CompileNullableColumnSchemaAnalyzerResultsIterator(ColumnSchemaAnalyzerCompilationContext context, Expression dataSchemaModelParameter, Expression resultVariable)
        {
            // IEnumerator<KeyValuePair<int, ElementDescriptor>> columnOffsetDescriptorEnumerator;
            Type elementDescriptorType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.ElementDescriptor", true);
            Type columnOffsetDescriptorType = typeof(KeyValuePair<,>).MakeGenericType(typeof(int), elementDescriptorType);
            ParameterExpression columnOffsetDescriptorEnumeratorVariable = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(columnOffsetDescriptorType), "columnOffsetDescriptorEnumerator");
            context.Variables.Add(columnOffsetDescriptorEnumeratorVariable);

            // columnOffsetDescriptorEnumerator = nullableColumnSchemaAnalyzer._columnOffsetToDescriptorMap.GetEnumerator();
            Expression columnOffsetToDescriptorMapField = Expression.Field(context.Analyzer, "_columnOffsetToDescriptorMap");
            Expression columnOffsetDescriptorEnumeratorValue = Expression.Call(columnOffsetToDescriptorMapField, typeof(IEnumerable<>).MakeGenericType(columnOffsetDescriptorType).GetMethod(nameof(IEnumerable<object>.GetEnumerator)));
            Expression columnOffsetDescriptorEnumeratorAssign = Expression.Assign(columnOffsetDescriptorEnumeratorVariable, columnOffsetDescriptorEnumeratorValue);

            // KeyValuePair<int, ElementDescriptor> columnOffsetDescriptor = columnOffsetDescriptorEnumerator.Current;
            ParameterExpression columnOffsetDescriptorVariable = Expression.Variable(columnOffsetDescriptorType, "columnOffsetDescriptor");
            Expression columnOffsetDescriptorValue = Expression.Property(columnOffsetDescriptorEnumeratorVariable, nameof(IEnumerator<object>.Current));
            Expression columnOffsetDescriptorAssign = Expression.Assign(columnOffsetDescriptorVariable, columnOffsetDescriptorValue);

            // int offset = columnOffsetDescriptor.Key;
            ParameterExpression offsetVariable = Expression.Variable(typeof(int), "offset");
            Expression offsetValue = Expression.Property(columnOffsetDescriptorVariable, nameof(KeyValuePair<object, object>.Key));
            Expression offsetAssign = Expression.Assign(offsetVariable, offsetValue);

            // TSqlObject columnElement = columnOffsetDescriptor.Value.GetModelElement(dataSchemaModel);
            Expression columnOffsetDescriptorValueProperty = Expression.Property(columnOffsetDescriptorVariable, nameof(KeyValuePair<object, object>.Value));
            ParameterExpression columnElementVariable = Expression.Variable(TSqlObjectType, "columnElement");
            Expression columnElementValue = Expression.Call(columnOffsetDescriptorValueProperty, "GetModelElement", new Type[0], dataSchemaModelParameter);
            Expression columnElementAssign = Expression.Assign(columnElementVariable, columnElementValue);

            // ColumnElementDescriptor hit = new ColumnElementDescriptor(offset, columnElement);
            Type columnElementHitType = typeof(ColumnElementHit);
            ConstructorInfo columnElementHitCtor = columnElementHitType.GetConstructor(new[] { typeof(int), TSqlObjectType });
            Guard.IsNotNull(columnElementHitCtor, nameof(columnElementHitCtor), "Could not find constructor on ColumnElementHit");
            ParameterExpression hitVariable = Expression.Variable(columnElementHitType, "hit");
            Expression hitValue = Expression.New(columnElementHitCtor, offsetVariable, columnElementVariable);
            Expression hitAssign = Expression.Assign(hitVariable, hitValue);

            // result.Hits.Add(hit);
            Expression hitsProperty = Expression.Property(resultVariable, nameof(ColumnSchemaAnalyzerResult.Hits));
            Expression hitsAddCall = Expression.Call(hitsProperty, nameof(ICollection<object>.Add), new Type[0], hitVariable);

            // while (columnOffsetDescriptorEnumerator.MoveNext())
            // {
            //     ...
            // }
            Expression propertyEnumeratorMoveNextCall = Expression.Call(columnOffsetDescriptorEnumeratorVariable, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext)));
            Expression enumeratorIfCondition = Expression.Equal(propertyEnumeratorMoveNextCall, Expression.Constant(true));
            Expression enumeratorIfTrue = Expression.Block
            (
                new[]
                {
                    columnOffsetDescriptorVariable
                  , offsetVariable
                  , columnElementVariable
                  , hitVariable
                }
              , columnOffsetDescriptorAssign
              , offsetAssign
              , columnElementAssign
              , hitAssign
              , hitsAddCall
            );
            LabelTarget enumeratorBreakLabel = Expression.Label("BreakColumnOffsetDescriptorEnumerator");
            Expression enumeratorIfFalse = Expression.Break(enumeratorBreakLabel);
            Expression enumeratorIfElse = Expression.IfThenElse(enumeratorIfCondition, enumeratorIfTrue, enumeratorIfFalse);
            Expression enumeratorLoop = Expression.Loop(enumeratorIfElse, enumeratorBreakLabel);

            // try
            // {
            //     ...
            // }
            // finally
            // {
            //     columnOffsetDescriptorEnumerator.Dispose();
            // }
            Expression tryBlock = Expression.Block(columnOffsetDescriptorEnumeratorAssign, enumeratorLoop);
            Expression @finally = Expression.Call(columnOffsetDescriptorEnumeratorVariable, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)));
            Expression tryFinally = Expression.TryFinally(tryBlock, @finally);
            context.Statements.Add(tryFinally);
        }

        private static void CompileSqlInterpretationVisitor90Results(ColumnSchemaAnalyzerCompilationContext context, Expression dataSchemaModelParameter, Expression sqlFragmentParameter, Expression resultVariable)
        {
            // SqlSchemaModel internalModel = SchemaAnalysisService.GetInternalModel(model);
            Type sqlSchemaModelType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlSchemaModel", true);
            ParameterExpression internalModelVariable = Expression.Variable(sqlSchemaModelType, "internalModel");
            Expression internalModelValue = Expression.Call(SchemaAnalysisServiceType, "GetInternalModel", new Type[0], dataSchemaModelParameter);
            Expression internalModelAssign = Expression.Assign(internalModelVariable, internalModelValue);
            context.Variables.Add(internalModelVariable);
            context.Statements.Add(internalModelAssign);

            // SqlInterpreterConstructor serviceConstructor = internalModel.DatabaseSchemaProvider.GetServiceConstructor<SqlInterpreterConstructor>();
            Type sqlInterpreterConstructorType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlInterpreterConstructor", true);
            Expression databaseSchemaProviderProperty = Expression.Property(internalModelVariable, "DatabaseSchemaProvider");
            ParameterExpression serviceConstructorVariable = Expression.Variable(sqlInterpreterConstructorType, "serviceConstructor");
            Expression serviceConstructorValue = Expression.Call(databaseSchemaProviderProperty, "GetServiceConstructor", new[] { sqlInterpreterConstructorType });
            Expression serviceConstructorAssign = Expression.Assign(serviceConstructorVariable, serviceConstructorValue);
            context.Variables.Add(serviceConstructorVariable);
            context.Statements.Add(serviceConstructorAssign);

            // serviceConstructor.Comparer = internalModel.Comparer;
            Expression serviceConstructorComparerProperty = Expression.Property(serviceConstructorVariable, "Comparer");
            Expression internalModelComparerProperty = Expression.Property(internalModelVariable, "Comparer");
            Expression comparerAssign = Expression.Assign(serviceConstructorComparerProperty, internalModelComparerProperty);
            context.Statements.Add(comparerAssign);

            // SqlInterpreter sqlInterpreter = serviceConstructor.ConstructService();
            Type sqlInterpreterType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlInterpreter", true);
            ParameterExpression sqlInterpreterVariable = Expression.Variable(sqlInterpreterType, "sqlInterpreter");
            Expression sqlInterpreterValue = Expression.Call(serviceConstructorVariable, "ConstructService", new Type[0]);
            Expression sqlInterpreterAssign = Expression.Assign(sqlInterpreterVariable, sqlInterpreterValue);
            context.Variables.Add(sqlInterpreterVariable);
            context.Statements.Add(sqlInterpreterAssign);

            // SelectStatementInterpretationVisitor interpretationVisitor = sqlInterpreter.InterpretationContext.VisitorFactory.CreateSelectStatementInterpretationVisitor(schemaAnalysisService._internalAnalyzer, sqlInterpreter.InterpretationContext);
            Type selectStatementInterpretationVisitorType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.SchemaModel.InterpretationVisitors.SelectStatementInterpretationVisitor", true);
            Expression interpretationContextProperty = Expression.Property(sqlInterpreterVariable, "InterpretationContext");
            Expression visitorFactoryProperty = Expression.Property(interpretationContextProperty, "VisitorFactory");
            Expression internalAnalyzerField = Expression.Field(context.SchemaAnalysisService, "_internalAnalyzer");
            ParameterExpression interpretationVisitorVariable = Expression.Variable(selectStatementInterpretationVisitorType, "interpretationVisitor");
            Expression interpretationVisitorValue = Expression.Call(visitorFactoryProperty, "CreateSelectStatementInterpretationVisitor", new Type[0], internalAnalyzerField, interpretationContextProperty);
            Expression interpretationVisitorAssign = Expression.Assign(interpretationVisitorVariable, interpretationVisitorValue);
            context.Variables.Add(interpretationVisitorVariable);
            context.Statements.Add(interpretationVisitorAssign);

            // sqlFragment.Accept(interpretationVisitor);
            Expression acceptCall = Expression.Call(sqlFragmentParameter, "Accept", new Type[0], interpretationVisitorVariable);
            context.Statements.Add(acceptCall);

            // result.Errors.AddRange(interpretationVisitor.InterpretationErrors.Select(InternalModelUtils.CreatExtensibilityError));
            Type interpretationErrorType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.SchemaModel.InterpretationError", true);
            Type internalModelUtils = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.InternalModelUtils", true);
            Type selectorType = typeof(Func<,>).MakeGenericType(interpretationErrorType, typeof(ExtensibilityError));
            MethodInfo selectMethod = GenericMethodUtility.EnumerableSelectMethod.MakeGenericMethod(interpretationErrorType, typeof(ExtensibilityError));
            MethodInfo creatExtensibilityErrorMethod = internalModelUtils.GetMethod("CreatExtensibilityError", BindingFlags.NonPublic | BindingFlags.Static);
            Guard.IsNotNull(creatExtensibilityErrorMethod, nameof(creatExtensibilityErrorMethod), "Could not find method 'CreatExtensibilityError' on InternalModelUtils");
            Expression interpretationErrorsProperty = Expression.Property(interpretationVisitorVariable, "InterpretationErrors");
            Expression interpretationErrorToExtensibilityError = Expression.Constant(Delegate.CreateDelegate(selectorType, null, creatExtensibilityErrorMethod));
            Expression errorsProperty = Expression.Property(resultVariable, nameof(ColumnSchemaAnalyzerResult.Errors));
            Expression extensibilityErrors = Expression.Call(selectMethod, interpretationErrorsProperty, interpretationErrorToExtensibilityError);
            Expression interpretationErrorsAddRange = Expression.Call(typeof(CollectionExtensions), nameof(CollectionExtensions.AddRange), new[] { typeof(ExtensibilityError) }, errorsProperty, extensibilityErrors);
            context.Statements.Add(interpretationErrorsAddRange);

            // if (interpretationVisitor.ColumnResolvers.Count > 0)
            // {
            //     IEnumerator<ResolvedDescriptor> selectColumnsEnumerator;
            //     try
            //     {
            //         selectColumnsEnumerator = interpretationVisitor.ColumnResolvers[0].SelectColumns.GetEnumerator();
            //         while (selectColumnsEnumerator.MoveNext())
            //         {
            //             ResolvedDescriptor selectColumn = selectColumnsEnumerator.Current;
            //             if (selectColumn.Potentials.Count > 0 && selectColumn.Potentials[0].Relevance == SqlElementDescriptorRelevance.SelfId)
            //             {
            //                 int offset = selectColumn.Fragment.StartOffset;
            //                 TSqlObject columnElement = new SqlElementDescriptor(selectColumn.Potentials[0]).GetModelElement(dataSchemaModel);
            //                 ColumnElementDescriptor column = new ColumnElementDescriptor(offset, columnElement);
            //                 columns.Add(column);
            //             }
            //         }
            //     }
            //     finally
            //     {
            //         selectColumnsEnumerator.Dispose();
            //     }
            // }
            CompileSqlInterpretationVisitor90ResultsIterator(context, dataSchemaModelParameter, resultVariable, interpretationVisitorVariable);
        }

        private static void CompileSqlInterpretationVisitor90ResultsIterator(ColumnSchemaAnalyzerCompilationContext context, Expression dataSchemaModelParameter, Expression resultVariable, Expression interpretationVisitorVariable)
        {
            // IEnumerator<ResolvedDescriptor> selectColumnsEnumerator;
            Type resolvedDescriptorType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.SchemaModel.ResolvedDescriptor", true);
            ParameterExpression selectColumnsEnumeratorVariable = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(resolvedDescriptorType), "selectColumnsEnumerator");
            
            // selectColumnsEnumerator = interpretationVisitor.ColumnResolvers[0].SelectColumns.GetEnumerator();
            Expression columnResolversProperty = Expression.Property(interpretationVisitorVariable, "ColumnResolvers");
            Expression firstColumnResolver = Expression.Property(columnResolversProperty, "Item", Expression.Constant(0));
            Expression selectColumnsProperty = Expression.Property(firstColumnResolver, "SelectColumns");
            Expression selectColumnsEnumeratorValue = Expression.Call(selectColumnsProperty, typeof(IEnumerable<>).MakeGenericType(resolvedDescriptorType).GetMethod(nameof(IEnumerable<object>.GetEnumerator)));
            Expression selectColumnsEnumeratorAssign = Expression.Assign(selectColumnsEnumeratorVariable, selectColumnsEnumeratorValue);

            // ResolvedDescriptor selectColumn = selectColumnsEnumerator.Current;
            ParameterExpression selectColumnVariable = Expression.Variable(resolvedDescriptorType, "selectColumn");
            Expression selectColumnValue = Expression.Property(selectColumnsEnumeratorVariable, nameof(IEnumerator<object>.Current));
            Expression selectColumnAssign = Expression.Assign(selectColumnVariable, selectColumnValue);

            // int offset = selectColumn.Fragment.StartOffset;
            Expression fragmentProperty = Expression.Property(selectColumnVariable, "Fragment");
            ParameterExpression offsetVariable = Expression.Variable(typeof(int), "offset");
            Expression offsetValue = Expression.Property(fragmentProperty, nameof(TSqlFragment.StartOffset));
            Expression offsetAssign = Expression.Assign(offsetVariable, offsetValue);

            // TSqlObject columnElement = new SqlElementDescriptor(selectColumn.Potentials[0]).GetModelElement(dataSchemaModel);
            Type schemaSqlElementDescriptorType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlElementDescriptor", true);
            Type dacSqlElementDescriptorType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.SqlElementDescriptor", true);
            ConstructorInfo dacSqlElementDescriptorCtor = dacSqlElementDescriptorType.GetConstructor(new[] { schemaSqlElementDescriptorType });
            Guard.IsNotNull(dacSqlElementDescriptorCtor, nameof(dacSqlElementDescriptorCtor), "Could not find constructor on SqlElementDescriptor");
            Expression potentialsProperty = Expression.Property(selectColumnVariable, "Potentials");
            Expression firstPotentialElement = Expression.Property(potentialsProperty, "Item", Expression.Constant(0));
            Expression sqlElementDescriptor = Expression.New(dacSqlElementDescriptorCtor, firstPotentialElement);
            ParameterExpression columnElementVariable = Expression.Variable(TSqlObjectType, "columnElement");
            Expression columnElementValue = Expression.Call(sqlElementDescriptor, "GetModelElement", new Type[0], dataSchemaModelParameter);
            Expression columnElementAssign = Expression.Assign(columnElementVariable, columnElementValue);

            // ColumnElementDescriptor hit = new ColumnElementDescriptor(offset, columnElement);
            Type columnElementHitType = typeof(ColumnElementHit);
            ConstructorInfo columnElementHitCtor = columnElementHitType.GetConstructor(new[] { typeof(int), TSqlObjectType });
            Guard.IsNotNull(columnElementHitCtor, nameof(columnElementHitCtor), "Could not find constructor on ColumnElementHit");
            ParameterExpression hitVariable = Expression.Variable(columnElementHitType, "column");
            Expression hitValue = Expression.New(columnElementHitCtor, offsetVariable, columnElementVariable);
            Expression hitAssign = Expression.Assign(hitVariable, hitValue);

            // result.Hits.Add(hit);
            Expression hitsProperty = Expression.Property(resultVariable, nameof(ColumnSchemaAnalyzerResult.Hits));
            Expression hitsAddCall = Expression.Call(hitsProperty, nameof(ICollection<object>.Add), new Type[0], hitVariable);

            // if (selectColumn.Potentials.Count > 0 && selectColumn.Potentials[0].Relevance == SqlElementDescriptorRelevance.SelfId)
            // {
            //     ...
            // }
            Expression selectColumnBlock = Expression.Block
            (
                new[]
                {
                    offsetVariable
                  , columnElementVariable
                  , hitVariable
                }
              , offsetAssign
              , columnElementAssign
              , hitAssign
              , hitsAddCall
            );
            Type sqlElementDescriptorRelevanceType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlElementDescriptorRelevance", true);
            object sqlElementDescriptorRelevanceSelfId = Enum.Parse(sqlElementDescriptorRelevanceType, "SelfId");
            Expression isEmptyProperty = Expression.Property(firstPotentialElement, "IsEmpty");
            Expression relevanceProperty = Expression.Property(firstPotentialElement, "Relevance");
            Expression selectColumnConditionLeft = Expression.Not(isEmptyProperty);
            Expression selectColumnConditionRight = Expression.Equal(relevanceProperty, Expression.Constant(sqlElementDescriptorRelevanceSelfId));
            Expression selectColumnCondition = Expression.And(selectColumnConditionLeft, selectColumnConditionRight);
            Expression selectColumnConditionBlock = Expression.IfThen(selectColumnCondition, selectColumnBlock);

            // while (selectColumnsEnumerator.MoveNext())
            // {
            //     ...
            // }
            Expression propertyEnumeratorMoveNextCall = Expression.Call(selectColumnsEnumeratorVariable, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext)));
            Expression enumeratorIfCondition = Expression.Equal(propertyEnumeratorMoveNextCall, Expression.Constant(true));
            Expression enumeratorIfTrue = Expression.Block
            (
                new[]
                {
                    selectColumnVariable
                }
              , selectColumnAssign
              , selectColumnConditionBlock
            );
            LabelTarget enumeratorBreakLabel = Expression.Label("BreakSelectColumnsEnumerator");
            Expression enumeratorIfFalse = Expression.Break(enumeratorBreakLabel);
            Expression enumeratorIfElse = Expression.IfThenElse(enumeratorIfCondition, enumeratorIfTrue, enumeratorIfFalse);
            Expression enumeratorLoop = Expression.Loop(enumeratorIfElse, enumeratorBreakLabel);

            // try
            // {
            //     ...
            // }
            // finally
            // {
            //     selectColumnsEnumerator.Dispose();
            // }
            Expression tryBlock = Expression.Block(selectColumnsEnumeratorAssign, enumeratorLoop);
            Expression @finally = Expression.Call(selectColumnsEnumeratorVariable, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)));
            Expression tryFinally = Expression.TryFinally(tryBlock, @finally);

            // if (interpretationVisitor.ColumnResolvers.Count > 0)
            // {
            //     ...
            // }
            Expression columnResolversCountProperty = Expression.Property(columnResolversProperty, nameof(List<object>.Count));
            Expression columnResolversIfCondition = Expression.GreaterThan(columnResolversCountProperty, Expression.Constant(0));
            Expression columnResolversIfTrue = Expression.Block(new[] { selectColumnsEnumeratorVariable }, tryFinally);
            Expression columnResolversIf = Expression.IfThen(columnResolversIfCondition, columnResolversIfTrue);
            context.Statements.Add(columnResolversIf);
        }

        private sealed class ColumnSchemaAnalyzerCompilationContext
        {
            public ICollection<ParameterExpression> Variables { get; }
            public ICollection<Expression> Statements { get; }
            public Expression Analyzer { get; set; }
            public Expression SchemaAnalysisService { get; set; }

            public ColumnSchemaAnalyzerCompilationContext()
            {
                this.Variables = new Collection<ParameterExpression>();
                this.Statements = new Collection<Expression>();
            }
        }

        private delegate ColumnSchemaAnalyzerResult AnalyzeColumns(TSqlModel dataSchemaModel, TSqlObject modelElement, TSqlFragment sqlFragment);
    }
}