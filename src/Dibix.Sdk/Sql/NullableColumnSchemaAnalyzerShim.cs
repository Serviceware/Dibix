using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Assembly = System.Reflection.Assembly;

namespace Dibix.Sdk.Sql
{
    internal static class NullableColumnSchemaAnalyzerShim
    {
        private static readonly Assembly DacExtensionsAssembly = typeof(Microsoft.SqlServer.Dac.Model.Assembly).Assembly;
        private static readonly Type ElementDescriptorType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.ElementDescriptor", true);
        private static readonly Type NullableColumnSchemaAnalyzerType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.Rules.Performance.NullableColumnSchemaAnalyzer");
        private static readonly Delegate VisitFragmentHandler = CompileVisitFragment();
        private static readonly Delegate VisitAmbiguousFragmentHandler = CompileVisitAmbiguousFragment();

        public static void Init() => SetupShims(nameof(BeginDdlStatement), nameof(VisitFragment), nameof(VisitAmbiguousFragment));

        public static void BeginDdlStatement(object instance, object fragment) { }
        
        public static void VisitFragment(object instance, object fragment, object sqlElementDescriptor, object relevance) => VisitFragmentHandler.DynamicInvoke(instance, fragment, sqlElementDescriptor);

        public static void VisitAmbiguousFragment(object instance, object fragment, object possibilities) => VisitAmbiguousFragmentHandler.DynamicInvoke(instance, fragment, possibilities);

        private static void SetupShims(params string[] methodNames)
        {
            foreach (string methodName in methodNames)
            {
                MethodInfo sourceMethod = SafeGetShimMethod(NullableColumnSchemaAnalyzerType, methodName);
                MethodInfo targetMethod = SafeGetShimMethod(typeof(NullableColumnSchemaAnalyzerShim), methodName);
                ReplaceMethodInMemory(sourceMethod, targetMethod);
            }
        }

        private static MethodInfo SafeGetShimMethod(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName);
            Guard.IsNotNull(method, nameof(method), $"Could not find method '{methodName}' on type {type.Name}");
            return method;
        }

        private static void ReplaceMethodInMemory(MethodInfo sourceMethod, MethodInfo targetMethod)
        {
            unsafe
            {
                ulong* methodDesc = (ulong*)sourceMethod.MethodHandle.Value.ToPointer();
                int index = (int)((*methodDesc >> 32) & 0xFF);
                if (IntPtr.Size == 4)
                {
                    uint* classStart = (uint*)sourceMethod.DeclaringType.TypeHandle.Value.ToPointer();
                    classStart += 10;
                    classStart = (uint*)*classStart;
                    uint* tar = classStart + index;

                    uint* inj = (uint*)targetMethod.MethodHandle.Value.ToPointer() + 2;
                    *tar = *inj;
                }
                else
                {
                    ulong* classStart = (ulong*)sourceMethod.DeclaringType.TypeHandle.Value.ToPointer();
                    classStart += 8;
                    classStart = (ulong*)*classStart;
                    ulong* tar = classStart + index;

                    ulong* inj = (ulong*)targetMethod.MethodHandle.Value.ToPointer() + 1;
                    *tar = *inj;
                }
            }
        }

        private static Delegate CompileVisitFragment()
        {
            // (NullableColumnSchemaAnalyzer instance, TSqlFragment fragment, ElementDescriptor sqlElementDescriptor) =>
            ParameterExpression instanceParameter = Expression.Parameter(NullableColumnSchemaAnalyzerType, "instance");
            ParameterExpression fragmentParameter = Expression.Parameter(typeof(TSqlFragment), "fragment");
            ParameterExpression sqlElementDescriptorParameter = Expression.Parameter(ElementDescriptorType, "sqlElementDescriptor");

            // sqlElementDescriptor != null
            Expression sqlElementDescriptorNullCondition = Expression.NotEqual(sqlElementDescriptorParameter, Expression.Constant(null));

            // instance._columnOffsetToDescriptorMap[fragment.StartOffset] = sqlElementDescriptor;
            Expression columnOffsetToDescriptorMapField = Expression.Field(instanceParameter, "_columnOffsetToDescriptorMap");
            Expression startOffsetProperty = Expression.Property(fragmentParameter, nameof(TSqlFragment.StartOffset));
            Expression columnOffsetToDescriptorMapIndexer = Expression.Property(columnOffsetToDescriptorMapField, "Item", startOffsetProperty);
            Expression columnOffsetToDescriptorMapAssign = Expression.Assign(columnOffsetToDescriptorMapIndexer, sqlElementDescriptorParameter);

            // if (...)
            // {
            //     ...
            // }
            Expression sqlElementDescriptorNullIf = Expression.IfThen(sqlElementDescriptorNullCondition, columnOffsetToDescriptorMapAssign);

            LambdaExpression lambda = Expression.Lambda
            (
                sqlElementDescriptorNullIf
              , instanceParameter
              , fragmentParameter
              , sqlElementDescriptorParameter
            );
            Delegate compiled = lambda.Compile();
            return compiled;
        }

        private static Delegate CompileVisitAmbiguousFragment()
        {
            Type potentialElementDescriptorType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.PotentialElementDescriptor", true);
            Type possibilitiesType = typeof(IEnumerable<>).MakeGenericType(potentialElementDescriptorType);

            // (NullableColumnSchemaAnalyzer instance, TSqlFragment fragment, IEnumerable<PotentialElementDescriptor> possibilities) =>
            ParameterExpression instanceParameter = Expression.Parameter(NullableColumnSchemaAnalyzerType, "instance");
            ParameterExpression fragmentParameter = Expression.Parameter(typeof(TSqlFragment), "fragment");
            ParameterExpression possibilitiesParameter = Expression.Parameter(possibilitiesType, "possibilities");

            // if (this.TryGetModelElementFromPossibilities(possibilities, this.SchemaModel, out TSqlObject _, out elementDescriptor, out relevance))
            // {
            //     instance._columnOffsetToDescriptorMap[fragment.StartOffset] = sqlElementDescriptor;
            // }

            // this.TryGetModelElementFromPossibilities(possibilities, this.SchemaModel, out TSqlObject _, out elementDescriptor, out relevance)
            Type elementDescriptorRelevanceType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.ElementDescriptorRelevance", true);
            ParameterExpression elementVariable = Expression.Variable(typeof(TSqlObject), "_");
            ParameterExpression elementDescriptorVariable = Expression.Variable(ElementDescriptorType, "elementDescriptor");
            ParameterExpression relevanceVariable = Expression.Variable(elementDescriptorRelevanceType, "relevance");
            Expression schemaModelProperty = Expression.Property(instanceParameter, "SchemaModel");
            Expression tryGetModelElementFromPossibilitiesCall = Expression.Call
            (
                instanceParameter
              , "TryGetModelElementFromPossibilities"
              , new Type[0]
              , possibilitiesParameter
              , schemaModelProperty
              , elementVariable
              , elementDescriptorVariable
              , relevanceVariable
            );

            // instance._columnOffsetToDescriptorMap[fragment.StartOffset] = sqlElementDescriptor;
            Expression columnOffsetToDescriptorMapField = Expression.Field(instanceParameter, "_columnOffsetToDescriptorMap");
            Expression startOffsetProperty = Expression.Property(fragmentParameter, nameof(TSqlFragment.StartOffset));
            Expression columnOffsetToDescriptorMapIndexer = Expression.Property(columnOffsetToDescriptorMapField, "Item", startOffsetProperty);
            Expression columnOffsetToDescriptorMapAssign = Expression.Assign(columnOffsetToDescriptorMapIndexer, elementDescriptorVariable);

            // if (...)
            // {
            //     ...
            // }
            Expression tryGetModelElementFromPossibilitiesIf = Expression.IfThen(tryGetModelElementFromPossibilitiesCall, columnOffsetToDescriptorMapAssign);

            Expression block = Expression.Block
            (
                new[]
                {
                    elementVariable
                  , elementDescriptorVariable
                  , relevanceVariable
                }
                , tryGetModelElementFromPossibilitiesIf
            );

            LambdaExpression lambda = Expression.Lambda
            (
                block
              , instanceParameter
              , fragmentParameter
              , possibilitiesParameter
            );
            Delegate compiled = lambda.Compile();
            return compiled;
        }
    }
}