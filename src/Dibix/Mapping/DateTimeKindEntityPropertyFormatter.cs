using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Dibix
{
    internal sealed class DateTimeKindEntityPropertyFormatter : AttributedEntityPropertyFormatter<DateTimeKindAttribute>, IEntityPropertyFormatter
    {
        protected override IEnumerable<Expression> GetValueFormatterParameters(Expression valueParameter, DateTimeKindAttribute attribute)
        {
            yield return valueParameter;
            yield return Expression.Constant(attribute.Kind);
        }
        protected override Expression BuildExpression(IEnumerable<Expression> arguments)
        {
            Expression[] expressionsArray = arguments as Expression[] ?? arguments.ToArray();
            Expression call = Expression.Call(typeof(DateTimeKindEntityPropertyFormatter), nameof(SpecifyKind), new Type[0], expressionsArray);
            return call;
        }

        private static DateTime? SpecifyKind(DateTime? value, DateTimeKind kind) => value != null ? DateTime.SpecifyKind(value.Value, kind) : (DateTime?)null;
    }
}