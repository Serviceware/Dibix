using System;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    public interface IHttpParameterConverter
    {
        Type ExpectedInputType { get; }
        
        Expression ConvertValue(Expression value, Expression dependencyResolverParameter);
    }
}