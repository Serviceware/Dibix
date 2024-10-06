using System;
using System.Collections.Generic;
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
    internal static class SchemaAnalyzer
    {
        private static readonly Assembly DacExtensionsAssembly = typeof(Microsoft.SqlServer.Dac.Model.Assembly).Assembly;
        private static readonly Assembly SchemaSqlAssembly = typeof(IExtension).Assembly;
        private static readonly Type TSqlModelType = typeof(TSqlModel);
        private static readonly Type TSqlObjectType = typeof(TSqlObject);
        private static readonly Lazy<SqlAnalysisRule, ISqlAnalysisRuleMetadata> LazyExport = new Lazy<SqlAnalysisRule, ISqlAnalysisRuleMetadata>(() => null, new ExportCodeAnalysisRuleAttribute("0", null));
        private static readonly AnalyzeSchema Analyzer = CompileAnalyzer();

        public static SchemaAnalyzerResult Analyze(TSqlModel dataSchemaModel, TSqlFragment sqlFragment)
        {
            TSqlObject modelElement = dataSchemaModel.GetObject(ModelSchema.User, new ObjectIdentifier("sys"), DacQueryScopes.BuiltIn); // Dummy
            return Analyzer(dataSchemaModel, modelElement, sqlFragment);
        }

        private static AnalyzeSchema CompileAnalyzer()
        {
            // SchemaAnalyzerResult result = new SchemaAnalyzerResult();
            // ExtensionDescriptor<SqlAnalysisRule, ISqlAnalysisRuleMetadata> extensionDescriptor = new ExtensionDescriptor<SqlAnalysisRule, ISqlAnalysisRuleMetadata>(SchemaAnalyzer.LazyExport);
            // RuleDescriptor ruleDescriptor = new RuleDescriptorImpl(extensionDescriptor);
            // NullableColumnSchemaAnalyzer nullableColumnSchemaAnalyzer = new NullableColumnSchemaAnalyzer(ruleDescriptor, dataSchemaModel, modelElement);
            // SchemaAnalysisService schemaAnalysisService = SchemaAnalysisService.CreateAnalysisService(nullableColumnSchemaAnalyzer);
            // IList<ExtensibilityError> interpretationErrors = schemaAnalysisService.AnalyzeScript(dataSchemaModel, sqlFragment);
            // result.Errors.AddRange(interpretationErrors);
            // result.DDLStatements.AddRange(NullableColumnSchemaAnalyzerProxy.GetDDLStatements(nullableColumnSchemaAnalyzer)));
            // result.Locations.AddRange(NullableColumnSchemaAnalyzerProxy.GetElementLocationMap(nullableColumnSchemaAnalyzer));
            // return result;

            // (TSqlModel dataSchemaModel, TSqlObject modelElement, TSqlFragement sqlFragment) => 
            ParameterExpression dataSchemaModelParameter = Expression.Parameter(TSqlModelType, "dataSchemaModel");
            ParameterExpression modelElementParameter = Expression.Parameter(TSqlObjectType, "modelElement");
            ParameterExpression sqlFragmentParameter = Expression.Parameter(typeof(TSqlFragment), "sqlFragment");

            // SchemaAnalyzerResult result = new SchemaAnalyzerResult();
            Type schemaAnalyzerResultType = typeof(SchemaAnalyzerResult);
            ParameterExpression resultVariable = Expression.Variable(schemaAnalyzerResultType, "result");
            Expression resultValue = Expression.New(schemaAnalyzerResultType);
            Expression resultAssign = Expression.Assign(resultVariable, resultValue);

            // ExtensionDescriptor<SqlAnalysisRule, ISqlAnalysisRuleMetadata> extensionDescriptor = new ExtensionDescriptor<SqlAnalysisRule, ISqlAnalysisRuleMetadata>(SchemaAnalyzer.LazyExport);
            Expression lazyExport = Expression.Field(null, typeof(SchemaAnalyzer), nameof(LazyExport));
            Type extensionDescriptorType = SchemaSqlAssembly.GetType("Microsoft.SqlServer.Dac.Extensibility.ExtensionDescriptor`2", true)
                                                            .MakeGenericType(typeof(SqlAnalysisRule), typeof(ISqlAnalysisRuleMetadata));
            ConstructorInfo extensionDescriptorCtor = extensionDescriptorType.GetConstructorSafe(lazyExport.Type);
            Guard.IsNotNull(extensionDescriptorCtor, nameof(extensionDescriptorCtor), "Could not find constructor on ExtensionDescriptor<,>");
            ParameterExpression extensionDescriptorVariable = Expression.Variable(extensionDescriptorType, "extensionDescriptor");
            Expression extensionDescriptorValue = Expression.New(extensionDescriptorCtor, lazyExport);
            Expression extensionDescriptorAssign = Expression.Assign(extensionDescriptorVariable, extensionDescriptorValue);

            // RuleDescriptor ruleDescriptor = new RuleDescriptorImpl(extensionDescriptor);
            Type ruleDescriptorType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.RuleDescriptor", true);
            Type ruleDescriptorImplType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.Engine.RuleDescriptorImpl", true);
            ConstructorInfo ruleDescriptorImplCtor = ruleDescriptorImplType.GetConstructorSafe(extensionDescriptorType);
            Guard.IsNotNull(ruleDescriptorImplCtor, nameof(ruleDescriptorImplCtor), "Could not find constructor on RuleDescriptorImpl");
            ParameterExpression ruleDescriptorVariable = Expression.Variable(ruleDescriptorType, "ruleDescriptor");
            Expression ruleDescriptorValue = Expression.New(ruleDescriptorImplCtor, extensionDescriptorVariable);
            Expression ruleDescriptorAssign = Expression.Assign(ruleDescriptorVariable, ruleDescriptorValue);

            // NullableColumnSchemaAnalyzer nullableColumnSchemaAnalyzer = new NullableColumnSchemaAnalyzer(ruleDescriptor, dataSchemaModel, modelElement);
            Type nullableColumnSchemaAnalyzerType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.Rules.Performance.NullableColumnSchemaAnalyzer");
            ConstructorInfo nullableColumnSchemaAnalyzerCtor = nullableColumnSchemaAnalyzerType.GetConstructorSafe(ruleDescriptorType, TSqlModelType, TSqlObjectType);
            Guard.IsNotNull(nullableColumnSchemaAnalyzerCtor, nameof(nullableColumnSchemaAnalyzerCtor), "Could not find constructor on NullableColumnSchemaAnalyzer");
            ParameterExpression nullableColumnSchemaAnalyzerVariable = Expression.Variable(nullableColumnSchemaAnalyzerType, "nullableColumnSchemaAnalyzer");
            Expression nullableColumnSchemaAnalyzerValue = Expression.New(nullableColumnSchemaAnalyzerCtor, ruleDescriptorVariable, dataSchemaModelParameter, modelElementParameter);
            Expression nullableColumnSchemaAnalyzerAssign = Expression.Assign(nullableColumnSchemaAnalyzerVariable, nullableColumnSchemaAnalyzerValue);

            // SchemaAnalysisService schemaAnalysisService = SchemaAnalysisService.CreateAnalysisService(nullableColumnSchemaAnalyzer);
            Type schemaAnalysisServiceType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.SchemaAnalysisService", true);
            ParameterExpression schemaAnalysisServiceVariable = Expression.Variable(schemaAnalysisServiceType, "schemaAnalysisService");
            Expression schemaAnalysisServiceValue = Expression.Call(schemaAnalysisServiceType, "CreateAnalysisService", Type.EmptyTypes, nullableColumnSchemaAnalyzerVariable);
            Expression schemaAnalysisServiceAssign = Expression.Assign(schemaAnalysisServiceVariable, schemaAnalysisServiceValue);

            // IList<ExtensibilityError> interpretationErrors = schemaAnalysisService.AnalyzeScript(dataSchemaModel, sqlFragment);
            ParameterExpression interpretationErrorsVariable = Expression.Variable(typeof(IList<ExtensibilityError>), "interpretationErrors");
            Expression interpretationErrorsValue = Expression.Call(schemaAnalysisServiceVariable, "AnalyzeScript", Type.EmptyTypes, dataSchemaModelParameter, sqlFragmentParameter);
            Expression interpretationErrorsAssign = Expression.Assign(interpretationErrorsVariable, interpretationErrorsValue);

            // result.Errors.AddRange(interpretationErrors);
            Expression errorsProperty = Expression.Property(resultVariable, nameof(SchemaAnalyzerResult.Errors));
            Expression interpretationErrorsAddRange = Expression.Call(typeof(CollectionExtensions), nameof(CollectionExtensions.AddRange), new[] { typeof(ExtensibilityError) }, errorsProperty, interpretationErrorsVariable);

            // result.DDLStatements.AddRange(NullableColumnSchemaAnalyzerProxy.GetDDLStatements(nullableColumnSchemaAnalyzer));
            Expression ddlStatementsProperty = Expression.Property(resultVariable, nameof(SchemaAnalyzerResult.DDLStatements));
            Expression ddlStatementsValue = Expression.Call(typeof(NullableColumnSchemaAnalyzerProxy), nameof(NullableColumnSchemaAnalyzerProxy.GetDDLStatements), Type.EmptyTypes, nullableColumnSchemaAnalyzerVariable);
            Expression ddlStatementsAddRange = Expression.Call(typeof(CollectionExtensions), nameof(CollectionExtensions.AddRange), new[] { typeof(TSqlFragment) }, ddlStatementsProperty, ddlStatementsValue);

            // result.Locations.AddRange(NullableColumnSchemaAnalyzerProxy.GetElementLocationMap(nullableColumnSchemaAnalyzer));
            Expression locationsProperty = Expression.Property(resultVariable, nameof(SchemaAnalyzerResult.Locations));
            Expression locationsValue = Expression.Call(typeof(NullableColumnSchemaAnalyzerProxy), nameof(NullableColumnSchemaAnalyzerProxy.GetElementLocationMap), Type.EmptyTypes, nullableColumnSchemaAnalyzerVariable);
            Expression locationsAddRange = Expression.Call(typeof(CollectionExtensions), nameof(CollectionExtensions.AddRange), new[] { typeof(KeyValuePair<int, ElementLocation>) }, locationsProperty, locationsValue);

            Expression block = Expression.Block
            (
                [
                    resultVariable
                  , extensionDescriptorVariable
                  , ruleDescriptorVariable
                  , nullableColumnSchemaAnalyzerVariable
                  , schemaAnalysisServiceVariable
                  , interpretationErrorsVariable
                ]
              , resultAssign
              , extensionDescriptorAssign
              , ruleDescriptorAssign
              , nullableColumnSchemaAnalyzerAssign
              , schemaAnalysisServiceAssign
              , interpretationErrorsAssign
              , interpretationErrorsAddRange
              , ddlStatementsAddRange
              , locationsAddRange
              , resultVariable
            );

            Expression<AnalyzeSchema> lambda = Expression.Lambda<AnalyzeSchema>
            (
                block
              , dataSchemaModelParameter
              , modelElementParameter
              , sqlFragmentParameter
            );
            AnalyzeSchema compiled = lambda.Compile();
            return compiled;
        }

        private delegate SchemaAnalyzerResult AnalyzeSchema(TSqlModel dataSchemaModel, TSqlObject modelElement, TSqlFragment sqlFragment);
    }
}