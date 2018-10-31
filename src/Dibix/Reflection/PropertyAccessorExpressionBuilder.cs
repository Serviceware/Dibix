using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix
{
    internal static class PropertyAccessorExpressionBuilder
    {
        public static Func<object, object> BuildValueGetter(PropertyInfo property)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            MethodInfo method = property.GetGetMethod();
            Expression body;
            if (method != null)
            {
                Expression instanceParameterCast = Expression.Convert(instanceParameter, property.DeclaringType);
                Expression getterExpression = Expression.Call(instanceParameterCast, property.GetGetMethod());
                body = Expression.Convert(getterExpression, typeof(object));
            }
            else
            {
                string message = $"Property '{property.Name}' on type '{property.DeclaringType}' has no getter";
                body = BuildExceptionExpression(message);
            }

            return Expression.Lambda<Func<object, object>>(body, instanceParameter).Compile();
        }

        public static Action<object, object> BuildValueSetter(PropertyInfo property)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            ParameterExpression valueParameter = Expression.Parameter(typeof(object), "value");
            MethodInfo method = property.GetSetMethod();
            Expression body;
            if (method != null)
            {
                Expression instanceParameterCast = Expression.Convert(instanceParameter, property.DeclaringType);
                Expression valueParameterCast = Expression.Convert(valueParameter, property.PropertyType);
                body = Expression.Call(instanceParameterCast, property.GetSetMethod(), valueParameterCast);
            }
            else
            {
                string message = $"Property '{property.Name}' on type '{property.DeclaringType}' has no setter";
                body = BuildExceptionExpression(message);
            }

            return Expression.Lambda<Action<object, object>>(body, instanceParameter, valueParameter).Compile();
        }

        private static Expression BuildExceptionExpression(string message)
        {
            Expression exception = Expression.Constant(new NotImplementedException(message));
            Expression @throw = Expression.Throw(exception);
            return Expression.Block(@throw, Expression.Constant(null, typeof(object)));
        }
    }
}
