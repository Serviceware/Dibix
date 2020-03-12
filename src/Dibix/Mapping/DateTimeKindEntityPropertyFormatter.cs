using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix
{
    internal sealed class DateTimeKindEntityPropertyFormatter : AttributedEntityPropertyFormatter<DateTimeKindAttribute>, IEntityPropertyFormatter
    {
        protected override MethodInfo GetValueFormatterMethod() => typeof(DateTime).GetRuntimeMethod(nameof(DateTime.SpecifyKind), new[] { typeof(DateTime), typeof(DateTimeKind) });
        protected override IEnumerable<Expression> GetValueFormatterParameters(Expression valueParameter, DateTimeKindAttribute attribute)
        {
            yield return valueParameter;
            yield return Expression.Constant(attribute.Kind);
        }
    }
}