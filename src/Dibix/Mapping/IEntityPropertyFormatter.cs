using System.Linq.Expressions;
using System.Reflection;

namespace Dibix
{
    public interface IEntityPropertyFormatter
    {
        bool RequiresFormatting(PropertyInfo property);
        Expression BuildFormattingExpression(PropertyInfo propertyInfo, Expression propertyExpression);
    }
}