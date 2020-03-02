using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Dibix.Sdk.Utilities;
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
        private static readonly Type ElementDescriptorType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.ElementDescriptor", true);
        private static readonly Lazy<SqlAnalysisRule, ISqlAnalysisRuleMetadata> LazyExport = new Lazy<SqlAnalysisRule, ISqlAnalysisRuleMetadata>(() => null, new ExportCodeAnalysisRuleAttribute("0", null));
        private static readonly AnalyzeSchema Analyzer = CompileAnalyzer();

        static SchemaAnalyzer() => NullableColumnSchemaAnalyzerShim.Init();

        public static SchemaAnalyzerResult Analyze(TSqlModel dataSchemaModel, TSqlFragment sqlFragment)
        {
            TSqlObject modelElement = dataSchemaModel.GetObject(ModelSchema.User, new ObjectIdentifier("sys"), DacQueryScopes.BuiltIn);
            return Analyzer(dataSchemaModel, modelElement, sqlFragment);
        }

        private static AnalyzeSchema CompileAnalyzer()
        {
            ParameterExpression dataSchemaModelParameter = Expression.Parameter(TSqlModelType, "dataSchemaModel");
            ParameterExpression modelElementParameter = Expression.Parameter(TSqlObjectType, "modelElement");
            ParameterExpression sqlFragmentParameter = Expression.Parameter(typeof(TSqlFragment), "sqlFragment");

            // SchemaAnalyzerResult result = new SchemaAnalyzerResult();
            Type schemaAnalyzerResultType = typeof(SchemaAnalyzerResult);
            ParameterExpression resultVariable = Expression.Variable(schemaAnalyzerResultType, "result");
            Expression resultValue = Expression.New(schemaAnalyzerResultType);
            Expression resultAssign = Expression.Assign(resultVariable, resultValue);

            // ExtensionDescriptor<SqlAnalysisRule, ISqlAnalysisRuleMetadata> extensionDescriptor = new ExtensionDescriptor<SqlAnalysisRule, ISqlAnalysisRuleMetadata>(Analyzer.LazyExport);
            Expression lazyExport = Expression.Field(null, typeof(SchemaAnalyzer), nameof(LazyExport));
            Type extensionDescriptorType = SchemaSqlAssembly.GetType("Microsoft.SqlServer.Dac.Extensibility.ExtensionDescriptor`2", true)
                                                            .MakeGenericType(typeof(SqlAnalysisRule), typeof(ISqlAnalysisRuleMetadata));
            ConstructorInfo extensionDescriptorCtor = extensionDescriptorType.GetConstructor(new[] { lazyExport.Type });
            Guard.IsNotNull(extensionDescriptorCtor, nameof(extensionDescriptorCtor), "Could not find constructor on ExtensionDescriptor<,>");
            ParameterExpression extensionDescriptorVariable = Expression.Variable(extensionDescriptorType, "extensionDescriptor");
            Expression extensionDescriptorValue = Expression.New(extensionDescriptorCtor, lazyExport);
            Expression extensionDescriptorAssign = Expression.Assign(extensionDescriptorVariable, extensionDescriptorValue);

            // RuleDescriptor ruleDescriptor = new RuleDescriptorImpl(extensionDescriptor);
            Type ruleDescriptorType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.RuleDescriptor", true);
            Type ruleDescriptorImplType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.Engine.RuleDescriptorImpl", true);
            ConstructorInfo ruleDescriptorImplCtor = ruleDescriptorImplType.GetConstructor(new[] { extensionDescriptorType });
            Guard.IsNotNull(ruleDescriptorImplCtor, nameof(ruleDescriptorImplCtor), "Could not find constructor on RuleDescriptorImpl");
            ParameterExpression ruleDescriptorVariable = Expression.Variable(ruleDescriptorType, "ruleDescriptor");
            Expression ruleDescriptorValue = Expression.New(ruleDescriptorImplCtor, extensionDescriptorVariable);
            Expression ruleDescriptorAssign = Expression.Assign(ruleDescriptorVariable, ruleDescriptorValue);

            // NullableColumnSchemaAnalyzer nullableColumnSchemaAnalyzer = new NullableColumnSchemaAnalyzer(ruleDescriptor, dataSchemaModel, modelElement);
            Type nullableColumnSchemaAnalyzerType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.Rules.Performance.NullableColumnSchemaAnalyzer");
            ConstructorInfo nullableColumnSchemaAnalyzerCtor = nullableColumnSchemaAnalyzerType.GetConstructor(new[] { ruleDescriptorType, TSqlModelType, TSqlObjectType });
            Guard.IsNotNull(nullableColumnSchemaAnalyzerCtor, nameof(nullableColumnSchemaAnalyzerCtor), "Could not find constructor on NullableColumnSchemaAnalyzer");
            ParameterExpression nullableColumnSchemaAnalyzerVariable = Expression.Variable(nullableColumnSchemaAnalyzerType, "nullableColumnSchemaAnalyzer");
            Expression nullableColumnSchemaAnalyzerValue = Expression.New(nullableColumnSchemaAnalyzerCtor, ruleDescriptorVariable, dataSchemaModelParameter, modelElementParameter);
            Expression nullableColumnSchemaAnalyzerAssign = Expression.Assign(nullableColumnSchemaAnalyzerVariable, nullableColumnSchemaAnalyzerValue);

            // SchemaAnalysisService schemaAnalysisService = SchemaAnalysisService.CreateAnalysisService(nullableColumnSchemaAnalyzer);
            Type schemaAnalysisServiceType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.SchemaAnalysisService", true);
            ParameterExpression schemaAnalysisServiceVariable = Expression.Variable(schemaAnalysisServiceType, "schemaAnalysisService");
            Expression schemaAnalysisServiceValue = Expression.Call(schemaAnalysisServiceType, "CreateAnalysisService", new Type[0], nullableColumnSchemaAnalyzerVariable);
            Expression schemaAnalysisServiceAssign = Expression.Assign(schemaAnalysisServiceVariable, schemaAnalysisServiceValue);

            // IList<ExtensibilityError> interpretationErrors = schemaAnalysisService.AnalyzeScript(model, sqlFragment);
            ParameterExpression interpretationErrorsVariable = Expression.Variable(typeof(IList<ExtensibilityError>), "interpretationErrors");
            Expression interpretationErrorsValue = Expression.Call(schemaAnalysisServiceVariable, "AnalyzeScript", new Type[0], dataSchemaModelParameter, sqlFragmentParameter);
            Expression interpretationErrorsAssign = Expression.Assign(interpretationErrorsVariable, interpretationErrorsValue);

            // result.Errors.AddRange(interpretationErrors);
            Expression errorsProperty = Expression.Property(resultVariable, nameof(SchemaAnalyzerResult.Errors));
            Expression interpretationErrorsAddRange = Expression.Call(typeof(CollectionExtensions), nameof(CollectionExtensions.AddRange), new[] { typeof(ExtensibilityError) }, errorsProperty, interpretationErrorsVariable);

            // IEnumerator<KeyValuePair<int, ElementDescriptor>> columnOffsetDescriptorEnumerator;
            // try
            // {
            //     columnOffsetDescriptorEnumerator = nullableColumnSchemaAnalyzer._columnOffsetToDescriptorMap.GetEnumerator();
            //     while (columnOffsetDescriptorEnumerator.MoveNext())
            //     {
            //         KeyValuePair<int, ElementDescriptor> columnOffsetDescriptorElement = columnOffsetDescriptorEnumerator.Current;
            //         int offset = columnOffsetDescriptorElement.Key;
            //         ElementDescriptor elementDescriptor = columnOffsetDescriptorElement.Value;
            //         Func<TSqlModel, TSqlObject> elementAccessor = elementDescriptor.GetModelElement;
            //         ElementLocation location = new ElementLocation(offset, elementDescriptor.Identifiers, elementAccessor);
            //         result.Locations.Add(location);
            //     }
            // }
            // finally
            // {
            //     columnOffsetDescriptorEnumerator.Dispose();
            // }
            Type columnOffsetDescriptorType = typeof(KeyValuePair<,>).MakeGenericType(typeof(int), ElementDescriptorType);
            Expression columnOffsetToDescriptorMapField = Expression.Field(nullableColumnSchemaAnalyzerVariable, "_columnOffsetToDescriptorMap");
            ExpressionUtils.Foreach
            (
                "columnOffsetDescriptor"
              , columnOffsetToDescriptorMapField
              , columnOffsetDescriptorType
              , builder => CompileSchemaAnalyzerResultsIterator(resultVariable, builder)
              , out ParameterExpression enumeratorVariable
              , out Expression enumeratorStatement
            );

            Expression block = Expression.Block
            (
                new[]
                {
                    resultVariable
                  , extensionDescriptorVariable
                  , ruleDescriptorVariable
                  , nullableColumnSchemaAnalyzerVariable
                  , schemaAnalysisServiceVariable
                  , interpretationErrorsVariable
                  , enumeratorVariable
                }
              , resultAssign
              , extensionDescriptorAssign
              , ruleDescriptorAssign
              , nullableColumnSchemaAnalyzerAssign
              , schemaAnalysisServiceAssign
              , interpretationErrorsAssign
              , interpretationErrorsAddRange
              , enumeratorStatement
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

        private static void CompileSchemaAnalyzerResultsIterator(Expression resultVariable, IForeachBodyBuilder bodyBuilder)
        {
            // int offset = columnOffsetDescriptorElement.Key;
            ParameterExpression offsetVariable = Expression.Variable(typeof(int), "offset");
            Expression offsetValue = Expression.Property(bodyBuilder.Element, nameof(KeyValuePair<object, object>.Key));
            Expression offsetAssign = Expression.Assign(offsetVariable, offsetValue);
            bodyBuilder.AddAssignStatement(offsetVariable, offsetAssign);

            // ElementDescriptor elementDescriptor = columnOffsetDescriptorElement.Value;
            ParameterExpression elementDescriptorVariable = Expression.Variable(ElementDescriptorType, "elementDescriptor");
            Expression elementDescriptorValue = Expression.Property(bodyBuilder.Element, nameof(KeyValuePair<object, object>.Value));
            Expression elementDescriptorAsssign = Expression.Assign(elementDescriptorVariable, elementDescriptorValue);
            bodyBuilder.AddAssignStatement(elementDescriptorVariable, elementDescriptorAsssign);

            // Func<TSqlModel, TSqlObject> elementAccessor = elementDescriptor.GetModelElement;
            MethodInfo getModelElementMethod = ElementDescriptorType.GetMethod("GetModelElement");
            Guard.IsNotNull(getModelElementMethod, nameof(getModelElementMethod), "Could not find method 'GetModelElement' on ElementDescriptor");
            Type elementAccessorType = typeof(Func<TSqlModel, TSqlObject>);
            ParameterExpression elementAccessorVariable = Expression.Variable(elementAccessorType, "elementAccessor");
            Expression elementAccessorValue = Expression.Call(Expression.Constant(getModelElementMethod), "CreateDelegate", new Type[0], Expression.Constant(elementAccessorType), elementDescriptorVariable);
            Expression elementAccessorAssign = Expression.Assign(elementAccessorVariable, Expression.Convert(elementAccessorValue, elementAccessorType));
            bodyBuilder.AddAssignStatement(elementAccessorVariable, elementAccessorAssign);

            // ElementLocation location = new ElementLocation(offset, elementDescriptor.Identifiers, elementAccessor);
            Type elementLocationType = typeof(ElementLocation);
            ConstructorInfo elementLocationCtor = elementLocationType.GetConstructor(new[] { typeof(int), typeof(IEnumerable<string>), elementAccessorType });
            Guard.IsNotNull(elementLocationCtor, nameof(elementLocationCtor), "Could not find constructor on ElementLocation");
            Expression identifiersProperty = Expression.Property(elementDescriptorVariable, "Identifiers");
            ParameterExpression locationVariable = Expression.Variable(elementLocationType, "location");
            Expression locationValue = Expression.New(elementLocationCtor, offsetVariable, identifiersProperty, elementAccessorVariable);
            Expression locationAssign = Expression.Assign(locationVariable, locationValue);
            bodyBuilder.AddAssignStatement(locationVariable, locationAssign);

            // result.Locations.Add(location);
            Expression locationsProperty = Expression.Property(resultVariable, nameof(SchemaAnalyzerResult.Locations));
            Expression locationsAddCall = Expression.Call(locationsProperty, nameof(ICollection<object>.Add), new Type[0], locationVariable);
            bodyBuilder.AddStatement(locationsAddCall);
        }

        private delegate SchemaAnalyzerResult AnalyzeSchema(TSqlModel dataSchemaModel, TSqlObject modelElement, TSqlFragment sqlFragment);
    }
}