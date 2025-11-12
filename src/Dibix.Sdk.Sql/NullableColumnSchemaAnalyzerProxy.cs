using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HarmonyLib;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using BinaryExpression = System.Linq.Expressions.BinaryExpression;

namespace Dibix.Sdk.Sql
{
    internal static class NullableColumnSchemaAnalyzerProxy
    {
        private static readonly string HarmonyId = typeof(NullableColumnSchemaAnalyzerProxy).ToString();
        private static readonly System.Reflection.Assembly DacExtensionsAssembly = typeof(Microsoft.SqlServer.Dac.Model.Assembly).Assembly;
        private static readonly Type ElementDescriptorType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.ElementDescriptor", true);
        private static readonly Type NullableColumnSchemaAnalyzerType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.Rules.Performance.NullableColumnSchemaAnalyzer", true);
        private static readonly Type RuleSchemaAnalyzerType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.RuleSchemaAnalyzer", true);
        private static readonly Delegate BeginDdlStatementHandler = CompileBeginDdlStatement();
        private static readonly Delegate VisitFragmentHandler = CompileVisitFragment();
        private static readonly Delegate VisitAmbiguousFragmentHandler = CompileVisitAmbiguousFragment();
        private static readonly IDictionary<object, ICollection<TSqlFragment>> DDLStatementStore = new Dictionary<object, ICollection<TSqlFragment>>();
        private static readonly IDictionary<object, IDictionary<int, ElementLocation>> LocationsStore = new Dictionary<object, IDictionary<int, ElementLocation>>();
        private static Harmony _harmonyInstance;

        public static void Setup()
        {
            bool isInitialized = _harmonyInstance != null;
            if (isInitialized)
            {
                // The idea would be to restore the patches at the end of the usage, just for safety.
                // However, I haven't found a stable way that doesn't cause issues with unit tests when ran in the following order: CodeGeneration, CodeAnalysis.
                // Therefore, the current approach is to redirect the methods as early as possible and simulate the original method behavior to not break framework code analysis rules.
                return;
            }

            RedirectMethods();
        }

        public static void Remove()
        {
            RestoreMethods();

            if (DDLStatementStore.Any())
                throw new InvalidOperationException("There are still remaining DDL statements that haven't been collected");
        }

        public static void BeginDdlStatement(object __instance, object fragment)
        {
            if (!DDLStatementStore.TryGetValue(__instance, out ICollection<TSqlFragment> ddlStatements))
            {
                ddlStatements = new List<TSqlFragment>();
                DDLStatementStore.Add(__instance, ddlStatements);
            }
            ddlStatements.Add((TSqlFragment)fragment);

            BeginDdlStatementHandler.DynamicInvoke(__instance, fragment);
        }

        public static void VisitFragment(object __instance, object fragment, object sqlElementDescriptor, byte relevance)
        {
            VisitFragmentHandler.DynamicInvoke(__instance, fragment, sqlElementDescriptor, relevance);
        }

        public static void VisitAmbiguousFragment(object __instance, object fragment, object possibilities)
        {
            VisitAmbiguousFragmentHandler.DynamicInvoke(__instance, fragment, possibilities);
        }

        public static IEnumerable<TSqlFragment> GetDDLStatements(object instance)
        {
            IEnumerable<TSqlFragment> result = DDLStatementStore.TryGetValue(instance, out ICollection<TSqlFragment> ddlStatements) ? ddlStatements : Enumerable.Empty<TSqlFragment>();
            DDLStatementStore.Remove(instance);
            return result;
        }

        public static IDictionary<int, ElementLocation> GetElementLocationMap(object instance)
        {
            IDictionary<int, ElementLocation> result = LocationsStore.TryGetValue(instance, out IDictionary<int, ElementLocation> locations) ? locations : new Dictionary<int, ElementLocation>();
            LocationsStore.Remove(instance);
            return result;
        }

        private static void CollectElementLocation(object instance, ElementLocation elementLocation)
        {
            if (!LocationsStore.TryGetValue(instance, out IDictionary<int, ElementLocation> locations))
            {
                locations = new Dictionary<int, ElementLocation>();
                LocationsStore.Add(instance, locations);
            }
            locations[elementLocation.Offset] = elementLocation;
        }

        private static void RedirectMethods()
        {
            _harmonyInstance = new Harmony(HarmonyId);

            RedirectMethod(nameof(BeginDdlStatement));
            RedirectMethod(nameof(VisitFragment));
            RedirectMethod(nameof(VisitAmbiguousFragment));
        }

        private static void RedirectMethod(string methodName)
        {
            MethodInfo sourceMethod = NullableColumnSchemaAnalyzerType.SafeGetMethod(methodName);
            MethodInfo targetMethod = typeof(NullableColumnSchemaAnalyzerProxy).SafeGetMethod(methodName);
            _harmonyInstance.Patch(sourceMethod, prefix: new HarmonyMethod(targetMethod));
        }

        private static void RestoreMethods()
        {
            if (_harmonyInstance == null)
                throw new InvalidOperationException($"Harmony instance is not set. Has the method redirection been set up using the {nameof(Setup)}() method?");

            _harmonyInstance.UnpatchAll(_harmonyInstance.Id);
            _harmonyInstance = null;
        }

        private static Delegate CompileBeginDdlStatement()
        {
            // (NullableColumnSchemaAnalyzer instance, TSqlFragment fragment) =>
            ParameterExpression instanceParameter = Expression.Parameter(NullableColumnSchemaAnalyzerType, "instance");
            ParameterExpression fragmentParameter = Expression.Parameter(typeof(TSqlFragment), "fragment");

            // base.BeginDdlStatement(fragment);
            // if (TsqlScriptDomUtils.IsSubroutineViewOrTrigger(fragment))
            // {
            //     _predicatesWaitingForCheck.Clear();
            //     _columnOffsetToDescriptorMap.Clear();
            //     fragment.Accept(_whereClauseVisitor);
            // }

            // base.BeginDdlStatement(fragment);
            Expression beginDdlStatementCall = ExpressionUtility.CallBaseMethod(instanceParameter, RuleSchemaAnalyzerType, "BeginDdlStatement", fragmentParameter);

            // TsqlScriptDomUtils.IsSubroutineViewOrTrigger(fragment)
            Type tsqlScriptDomUtilsType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.CodeAnalysis.Internal.TsqlScriptDomUtils", true);
            Expression isSubroutineViewOrTriggerCall = Expression.Call(tsqlScriptDomUtilsType, "IsSubroutineViewOrTrigger", [], fragmentParameter);

            // _predicatesWaitingForCheck.Clear();
            Expression predicatesWaitingForCheckField = Expression.Field(instanceParameter, "_predicatesWaitingForCheck");
            Expression predicatesWaitingForCheckClearCall = Expression.Call(predicatesWaitingForCheckField, nameof(HashSet<object>.Clear), Type.EmptyTypes);

            // _columnOffsetToDescriptorMap.Clear();
            Expression columnOffsetToDescriptorMapField = Expression.Field(instanceParameter, "_columnOffsetToDescriptorMap");
            Expression columnOffsetToDescriptorMapClearCall = Expression.Call(columnOffsetToDescriptorMapField, nameof(Dictionary<object, object>.Clear), Type.EmptyTypes);

            // fragment.Accept(_whereClauseVisitor);
            Expression whereClauseVisitorField = Expression.Field(instanceParameter, "_whereClauseVisitor");
            Expression acceptCall = Expression.Call(fragmentParameter, nameof(TSqlFragment.Accept), [], whereClauseVisitorField);

            // if (...)
            // {
            //     ...
            // }
            Expression ifThenBlock = Expression.Block
            (
                predicatesWaitingForCheckClearCall
              , columnOffsetToDescriptorMapClearCall
              , acceptCall
            );
            Expression ifBlock = Expression.IfThen(isSubroutineViewOrTriggerCall, ifThenBlock);

            Expression block = Expression.Block
            (
                beginDdlStatementCall,
                ifBlock
            );

            LambdaExpression lambda = Expression.Lambda
            (
                block
              , instanceParameter
              , fragmentParameter
            );
            Delegate compiled = lambda.Compile();
            return compiled;
        }

        private static Delegate CompileVisitFragment()
        {
            Type elementDescriptorRelevanceType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.ElementDescriptorRelevance", true);

            // (NullableColumnSchemaAnalyzer instance, TSqlFragment fragment, ElementDescriptor sqlElementDescriptor, ElementDescriptorRelevance relevance) =>
            ParameterExpression instanceParameter = Expression.Parameter(NullableColumnSchemaAnalyzerType, "instance");
            ParameterExpression fragmentParameter = Expression.Parameter(typeof(TSqlFragment), "fragment");
            ParameterExpression sqlElementDescriptorParameter = Expression.Parameter(ElementDescriptorType, "sqlElementDescriptor");
            ParameterExpression relevanceParameter = Expression.Parameter(elementDescriptorRelevanceType, "relevance");

            // // User implementation
            // if (sqlElementDescriptor != null)
            // {
            //     Func<TSqlModel, TSqlObject> elementAccessor = sqlElementDescriptor.GetModelElement;
            //     ElementLocation location = new ElementLocation(fragment.StartOffset, sqlElementDescriptor.Identifiers, elementAccessor);
            //     NullableColumnSchemaAnalyzerProxy.CollectElementLocation(instance, location);
            // }
            //
            // // Framework implementation
            // base.VisitFragment(fragment, sqlElementDescriptor, relevance);
            // if (fragment is ColumnReferenceExpression && relevance == ElementDescriptorRelevance.SelfId)
            // {
            //     _columnOffsetToDescriptorMap[fragment.StartOffset] = sqlElementDescriptor;
            // }

            // sqlElementDescriptor != null
            Expression sqlElementDescriptorNullCondition = Expression.NotEqual(sqlElementDescriptorParameter, Expression.Constant(null));

            // Func<TSqlModel, TSqlObject> elementAccessor = sqlElementDescriptor.GetModelElement;
            MethodInfo getModelElementMethod = ElementDescriptorType.SafeGetMethod("GetModelElement");
            Type elementAccessorType = typeof(Func<TSqlModel, TSqlObject>);
            ParameterExpression elementAccessorVariable = Expression.Variable(elementAccessorType, "elementAccessor");
            Expression elementAccessorValue = Expression.Call(Expression.Constant(getModelElementMethod), nameof(MethodInfo.CreateDelegate), Type.EmptyTypes, Expression.Constant(elementAccessorType), sqlElementDescriptorParameter);
            Expression elementAccessorAssign = Expression.Assign(elementAccessorVariable, Expression.Convert(elementAccessorValue, elementAccessorType));

            // ElementLocation location = new ElementLocation(fragment.StartOffset, sqlElementDescriptor.Identifiers, elementAccessor);
            Type elementLocationType = typeof(ElementLocation);
            ConstructorInfo elementLocationCtor = elementLocationType.GetConstructorSafe(typeof(int), typeof(IEnumerable<string>), elementAccessorType);
            Expression identifiersProperty = Expression.Property(sqlElementDescriptorParameter, "Identifiers");
            Expression startOffsetProperty = Expression.Property(fragmentParameter, nameof(TSqlFragment.StartOffset));
            ParameterExpression locationVariable = Expression.Variable(elementLocationType, "location");
            Expression locationValue = Expression.New(elementLocationCtor, startOffsetProperty, identifiersProperty, elementAccessorVariable);
            Expression locationAssign = Expression.Assign(locationVariable, locationValue);

            // NullableColumnSchemaAnalyzerProxy.CollectElementLocation(instance, location);
            Expression collectElementLocationCall = Expression.Call(typeof(NullableColumnSchemaAnalyzerProxy), nameof(CollectElementLocation), Type.EmptyTypes, instanceParameter, locationVariable);

            // if (sqlElementDescriptor != null)
            // {
            //     ...
            // }
            Expression ifUserThenBlock = Expression.Block
            (
                [
                    elementAccessorVariable
                  , locationVariable
                ]
              , elementAccessorAssign
              , locationAssign
              , collectElementLocationCall
            );
            Expression ifUserBlock = Expression.IfThen(sqlElementDescriptorNullCondition, ifUserThenBlock);

            // base.VisitFragment(fragment, sqlElementDescriptor, relevance);
            Expression visitFragmentCall = ExpressionUtility.CallBaseMethod(instanceParameter, RuleSchemaAnalyzerType, "VisitFragment", fragmentParameter, sqlElementDescriptorParameter, relevanceParameter);

            // fragment is ColumnReferenceExpression && relevance == ElementDescriptorRelevance.SelfId
            Expression fragmentIsColumnReferenceExpression = Expression.TypeIs(fragmentParameter, typeof(ColumnReferenceExpression));
            Expression relevanceIsSelfId = Expression.Equal(relevanceParameter, Expression.Constant(Enum.Parse(elementDescriptorRelevanceType, "SelfId")));
            BinaryExpression frameworkCondition = Expression.And(fragmentIsColumnReferenceExpression, relevanceIsSelfId);

            // _columnOffsetToDescriptorMap[fragment.StartOffset] = sqlElementDescriptor;
            Expression columnOffsetToDescriptorMapField = Expression.Field(instanceParameter, "_columnOffsetToDescriptorMap");
            Expression columnOffsetToDescriptorMapIndexer = Expression.Property(columnOffsetToDescriptorMapField, "Item", startOffsetProperty);
            Expression columnOffsetToDescriptorMapAssign = Expression.Assign(columnOffsetToDescriptorMapIndexer, sqlElementDescriptorParameter);

            // if (fragment is ColumnReferenceExpression && relevance == ElementDescriptorRelevance.SelfId)
            // {
            //     ...
            // }
            Expression ifFrameworkThenBlock = Expression.Block
            (
                columnOffsetToDescriptorMapAssign
            );
            Expression ifFrameworkBlock = Expression.IfThen(frameworkCondition, ifFrameworkThenBlock);

            Expression block = Expression.Block
            (
                ifUserBlock,
                visitFragmentCall,
                ifFrameworkBlock
            );

            LambdaExpression lambda = Expression.Lambda
            (
                block
              , instanceParameter
              , fragmentParameter
              , sqlElementDescriptorParameter
              , relevanceParameter
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

            // // Framework implementation
            // base.VisitAmbiguousFragment(fragment, possibilities);
            //
            // // User implementation
            // if (RuleSchemaAnalyzer.TryGetModelElementFromPossibilities(possibilities, base.SchemaModel, out var _, out var elementDescriptor, out var relevance))
            // {
            //     // User implementation
            //     Func<TSqlModel, TSqlObject> elementAccessor = elementDescriptor.GetModelElement;
            //     ElementLocation location = new ElementLocation(fragment.StartOffset, elementDescriptor.Identifiers, elementAccessor);
            //     NullableColumnSchemaAnalyzerProxy.CollectElementLocation(instance, location);
            //
            //     // Framework implementation
            //     if (fragment is ColumnReferenceExpression && relevance == ElementDescriptorRelevance.SelfId)
            //     {
            //         _columnOffsetToDescriptorMap[fragment.StartOffset] = elementDescriptor;
            //     }
            // }

            // base.VisitAmbiguousFragment(fragment, possibilities);
            Expression visitAmbiguousFragmentCall = ExpressionUtility.CallBaseMethod(instanceParameter, RuleSchemaAnalyzerType, "VisitAmbiguousFragment", fragmentParameter, possibilitiesParameter);

            // RuleSchemaAnalyzer.TryGetModelElementFromPossibilities(possibilities, this.SchemaModel, out TSqlObject _, out elementDescriptor, out relevance)
            Type elementDescriptorRelevanceType = DacExtensionsAssembly.GetType("Microsoft.SqlServer.Dac.Model.ElementDescriptorRelevance", true);
            ParameterExpression elementVariable = Expression.Variable(typeof(TSqlObject), "_");
            ParameterExpression elementDescriptorVariable = Expression.Variable(ElementDescriptorType, "elementDescriptor");
            ParameterExpression relevanceVariable = Expression.Variable(elementDescriptorRelevanceType, "relevance");
            Expression schemaModelProperty = Expression.Property(instanceParameter, "SchemaModel");
            Expression tryGetModelElementFromPossibilitiesCall = Expression.Call
            (
                RuleSchemaAnalyzerType
              , "TryGetModelElementFromPossibilities"
              , Type.EmptyTypes
              , possibilitiesParameter
              , schemaModelProperty
              , elementVariable
              , elementDescriptorVariable
              , relevanceVariable
            );

            // Func<TSqlModel, TSqlObject> elementAccessor = elementDescriptor.GetModelElement;
            MethodInfo getModelElementMethod = ElementDescriptorType.SafeGetMethod("GetModelElement");
            Type elementAccessorType = typeof(Func<TSqlModel, TSqlObject>);
            ParameterExpression elementAccessorVariable = Expression.Variable(elementAccessorType, "elementAccessor");
            Expression elementAccessorValue = Expression.Call(Expression.Constant(getModelElementMethod), nameof(MethodInfo.CreateDelegate), Type.EmptyTypes, Expression.Constant(elementAccessorType), elementDescriptorVariable);
            Expression elementAccessorAssign = Expression.Assign(elementAccessorVariable, Expression.Convert(elementAccessorValue, elementAccessorType));

            // ElementLocation location = new ElementLocation(fragment.StartOffset, elementDescriptor.Identifiers, elementAccessor);
            Type elementLocationType = typeof(ElementLocation);
            ConstructorInfo elementLocationCtor = elementLocationType.GetConstructorSafe(typeof(int), typeof(IEnumerable<string>), elementAccessorType);
            Expression identifiersProperty = Expression.Property(elementDescriptorVariable, "Identifiers");
            Expression startOffsetProperty = Expression.Property(fragmentParameter, nameof(TSqlFragment.StartOffset));
            ParameterExpression locationVariable = Expression.Variable(elementLocationType, "location");
            Expression locationValue = Expression.New(elementLocationCtor, startOffsetProperty, identifiersProperty, elementAccessorVariable);
            Expression locationAssign = Expression.Assign(locationVariable, locationValue);

            // NullableColumnSchemaAnalyzerProxy.CollectElementLocation(instance, location);
            Expression collectElementLocationCall = Expression.Call(typeof(NullableColumnSchemaAnalyzerProxy), nameof(CollectElementLocation), Type.EmptyTypes, instanceParameter, locationVariable);

            // fragment is ColumnReferenceExpression && relevance == ElementDescriptorRelevance.SelfId
            Expression fragmentIsColumnReferenceExpression = Expression.TypeIs(fragmentParameter, typeof(ColumnReferenceExpression));
            Expression relevanceIsSelfId = Expression.Equal(relevanceVariable, Expression.Constant(Enum.Parse(elementDescriptorRelevanceType, "SelfId")));
            BinaryExpression frameworkCondition = Expression.And(fragmentIsColumnReferenceExpression, relevanceIsSelfId);

            // _columnOffsetToDescriptorMap[fragment.StartOffset] = elementDescriptor;
            Expression columnOffsetToDescriptorMapField = Expression.Field(instanceParameter, "_columnOffsetToDescriptorMap");
            Expression columnOffsetToDescriptorMapIndexer = Expression.Property(columnOffsetToDescriptorMapField, "Item", startOffsetProperty);
            Expression columnOffsetToDescriptorMapAssign = Expression.Assign(columnOffsetToDescriptorMapIndexer, elementDescriptorVariable);

            // if (fragment is ColumnReferenceExpression && relevance == ElementDescriptorRelevance.SelfId)
            // {
            //     ...
            // }
            Expression ifFrameworkThenBlock = Expression.Block
            (
                columnOffsetToDescriptorMapAssign
            );
            Expression ifFrameworkBlock = Expression.IfThen(frameworkCondition, ifFrameworkThenBlock);

            // if (RuleSchemaAnalyzer.TryGetModelElementFromPossibilities(possibilities, this.SchemaModel, out TSqlObject _, out elementDescriptor, out relevance))
            // {
            //     ...
            // }
            Expression ifThenBlock = Expression.Block
            (
                [
                    elementAccessorVariable
                  , locationVariable
                ]
              , elementAccessorAssign
              , locationAssign
              , collectElementLocationCall
              , ifFrameworkBlock
            );
            Expression ifBlock = Expression.IfThen(tryGetModelElementFromPossibilitiesCall, ifThenBlock);

            Expression block = Expression.Block
            (
                [
                    elementVariable
                  , elementDescriptorVariable
                  , relevanceVariable
                ]
              , visitAmbiguousFragmentCall
              , ifBlock
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