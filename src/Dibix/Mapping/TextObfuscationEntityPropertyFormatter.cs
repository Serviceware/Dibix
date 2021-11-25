using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix
{
    internal sealed class TextObfuscationEntityPropertyFormatter : AttributedEntityPropertyFormatter<ObfuscatedAttribute>, IEntityPropertyFormatter
    {
        protected override Expression BuildExpression(PropertyInfo property, IEnumerable<Expression> arguments)
        {
            Expression[] expressionsArray = arguments as Expression[] ?? arguments.ToArray();
            Expression call = Expression.Call(typeof(TextObfuscator), nameof(TextObfuscator.Deobfuscate), Type.EmptyTypes, expressionsArray);
            return call;
        }
    }
}