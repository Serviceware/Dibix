using System;
using System.Linq.Expressions;
using Microsoft.Data.Tools.Schema.Extensibility;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Assembly = System.Reflection.Assembly;

namespace Dibix.Sdk.Sql
{
    internal static class TSqlObjectScriptDomAccessor
    {
        private static readonly Assembly SchemaSqlAssembly = typeof(IExtension).Assembly;
        private static readonly Func<TSqlObject, TSqlFragment> ScriptDomAccessor = CompileScriptDomAccessor();

        public static TSqlFragment GetScriptDom(TSqlObject element)
        {
            return ScriptDomAccessor(element);
        }

        private static Func<TSqlObject, TSqlFragment> CompileScriptDomAccessor()
        {
            // (TSqlObject element) =>
            ParameterExpression elementParameter = Expression.Parameter(typeof(TSqlObject), "element");

            // ((IScriptSourcedModelElement)element.ContextObject).PrimarySource.ScriptDom
            Expression contextObjectProperty = Expression.Property(elementParameter, "ContextObject");
            Type scriptedSourcedModelElementType = SchemaSqlAssembly.GetType("Microsoft.Data.Tools.Schema.SchemaModel.IScriptSourcedModelElement", true);
            Expression scriptedSourcedModelElementCast = Expression.Convert(contextObjectProperty, scriptedSourcedModelElementType);
            Expression primarySourceProperty = Expression.Property(scriptedSourcedModelElementCast, "PrimarySource");
            Expression scriptDomProperty = Expression.Property(primarySourceProperty, "ScriptDom");

            Expression<Func<TSqlObject, TSqlFragment>> lambda = Expression.Lambda<Func<TSqlObject, TSqlFragment>>(scriptDomProperty, elementParameter);
            Func<TSqlObject, TSqlFragment> compiled = lambda.Compile();
            return compiled;
        }
    }
}
