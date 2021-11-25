using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix
{
    internal sealed class DateTimeKindEntityPropertyFormatter : AttributedEntityPropertyFormatter<DateTimeKindAttribute>, IEntityPropertyFormatter
    {
        protected override IEnumerable<Expression> GetValueFormatterParameters(Expression valueParameter, DateTimeKindAttribute attribute)
        {
            yield return valueParameter;
            yield return Expression.Constant(attribute.Kind);
        }

        protected override Expression BuildExpression(PropertyInfo property, IEnumerable<Expression> arguments)
        {
            Expression[] expressionsArray = arguments as Expression[] ?? arguments.ToArray();
            Type targetType = property.PropertyType == typeof(DateTime?) ? typeof(DateTimeKindEntityPropertyFormatter) : typeof(DateTime);
            Expression call = Expression.Call(targetType, nameof(SpecifyKind), Type.EmptyTypes, expressionsArray);
            return call;
        }

        private static DateTime? SpecifyKind(DateTime? value, DateTimeKind kind) => value != null ? DateTime.SpecifyKind(value.Value, kind) : (DateTime?)null;
    }
}