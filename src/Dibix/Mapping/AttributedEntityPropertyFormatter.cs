using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix
{
    public abstract class AttributedEntityPropertyFormatter<TAttribute> : IEntityPropertyFormatter where TAttribute : Attribute
    {
        public bool RequiresFormatting(PropertyInfo property) => property.IsDefined(typeof(TAttribute));

        public Expression BuildFormattingExpression(PropertyInfo propertyInfo, Expression propertyExpression)
        {
            TAttribute attribute = propertyInfo.GetCustomAttribute<TAttribute>();
            Guard.IsNotNull(attribute, nameof(attribute));

            MethodInfo formatValueMethod = this.GetValueFormatterMethod();
            Expression formatValueCall = Expression.Call(formatValueMethod, this.GetValueFormatterParameters(propertyExpression, attribute));
            return formatValueCall;
        }

        protected abstract MethodInfo GetValueFormatterMethod();

        protected virtual IEnumerable<Expression> GetValueFormatterParameters(Expression valueParameter, TAttribute attribute)
        {
            yield return valueParameter;
        }
    }
}