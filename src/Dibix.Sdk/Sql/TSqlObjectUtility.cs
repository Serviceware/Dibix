using System;
using System.Linq.Expressions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    internal static class TSqlObjectUtility
    {
        private static readonly Func<ObjectIdentifier, int> IdResolver = CompileIdResolver();

        public static bool IsExternal(this TSqlObject @object)
        {
            int id = IdResolver(@object.Name);
            return (id & 1) == 1;
        }

        private static Func<ObjectIdentifier, int> CompileIdResolver()
        {
            ParameterExpression nameParameter = Expression.Parameter(typeof(ObjectIdentifier), "name");

            Expression idProperty = Expression.Property(nameParameter, "Id");
            Expression<Func<ObjectIdentifier, int>> lambda = Expression.Lambda<Func<ObjectIdentifier, int>>(idProperty, nameParameter);
            Func<ObjectIdentifier, int> compiled = lambda.Compile();
            return compiled;
        }
    }
}