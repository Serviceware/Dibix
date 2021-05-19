using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix
{
    public abstract class AttributedEntityPropertyFormatter<TAttribute> : IEntityPropertyFormatter where TAttribute : Attribute
    {
        public bool RequiresFormatting(PropertyInfo property) => property.IsDefined(typeof(TAttribute));

        public Expression BuildExpression(PropertyInfo propertyInfo, Expression propertyExpression)
        {
            TAttribute attribute = propertyInfo.GetCustomAttribute<TAttribute>();
            Guard.IsNotNull(attribute, nameof(attribute));

            Expression formattedValueExpression = this.BuildExpression(propertyInfo, this.GetValueFormatterParameters(propertyExpression, attribute));
            return formattedValueExpression;
        }

        protected virtual IEnumerable<Expression> GetValueFormatterParameters(Expression valueParameter, TAttribute attribute)
        {
            yield return valueParameter;
        }

        protected abstract Expression BuildExpression(PropertyInfo property, IEnumerable<Expression> arguments);
    }
}