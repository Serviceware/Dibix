using System.Linq.Expressions;

namespace Dibix.Http
{
    public interface IHttpParameterConverter
    {
        Expression ConvertValue(Expression value);
    }
}