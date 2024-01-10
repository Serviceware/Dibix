using System;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    public interface IHttpParameterResolutionContext
    {
        IHttpActionDescriptor Action { get; }
        Expression RequestParameter { get; }
        Expression ArgumentsParameter { get; }
        Expression DependencyResolverParameter { get; }
        Expression ActionParameter { get; }
        string PropertyPath { get; }

        void ResolveUsingInstanceProperty(Type instanceType, Expression instanceValue, bool ensureNullPropagation);
        void ResolveUsingInstanceProperty(Type instanceType, Expression instanceValue, bool ensureNullPropagation, string propertyPath);
        void ResolveUsingValue(Expression value);
    }
}