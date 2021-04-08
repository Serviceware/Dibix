using System;
using System.Linq.Expressions;

namespace Dibix.Http
{
    public interface IHttpParameterResolutionContext
    {
        HttpActionDefinition Action { get; }
        Expression RequestParameter { get; }
        Expression ArgumentsParameter { get; }
        Expression DependencyResolverParameter { get; }
        string PropertyPath { get; }

        void ResolveUsingInstanceProperty(Type instanceType, Expression instanceValue, bool ensureNullPropagation);
        void ResolveUsingValue(Expression value);
    }
}