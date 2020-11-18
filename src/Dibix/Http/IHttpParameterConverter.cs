using System;
using System.Linq.Expressions;

namespace Dibix.Http
{
    public interface IHttpParameterConverter
    {
        Type ExpectedInputType { get; }

        Expression ConvertValue(Expression value);
    }
}