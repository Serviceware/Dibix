using System.Linq.Expressions;
using System.Reflection;

namespace Dibix
{
    public interface IEntityPropertyFormatter
    {
        bool RequiresFormatting(PropertyInfo property);
        Expression BuildExpression(PropertyInfo propertyInfo, Expression propertyExpression);
    }
}