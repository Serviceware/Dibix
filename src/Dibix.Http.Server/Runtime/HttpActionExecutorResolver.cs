using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    internal static class HttpActionExecutorResolver
    {
        private static readonly Lazy<PropertyAccessor> DebugViewAccessor = new Lazy<PropertyAccessor>(BuildDebugViewAccessor);

        public static IHttpActionExecutionMethod Compile(IHttpActionDescriptor action)
        {
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(IDictionary<string, object>), "arguments");

            ICollection<Expression> parameters = new Collection<Expression>();
            ICollection<ParameterExpression> variables = new Collection<ParameterExpression>();

            foreach (ParameterInfo parameter in action.Target.GetParameters())
            {
                if (parameter.IsOut)
                {
                    ParameterExpression variable = Expression.Variable(parameter.ParameterType.GetElementType(), parameter.Name);
                    variables.Add(variable);
                    parameters.Add(variable);
                }
                else
                {
                    parameters.Add(Expression.Call(typeof(HttpActionExecutorResolver), nameof(CollectParameter), new[] { parameter.ParameterType }, argumentsParameter, Expression.Constant(parameter.Name)));
                }
            }

            Expression result = Expression.Call(action.Target, parameters);
            if (result.Type == typeof(Task))
            {
                result = Expression.Call(typeof(HttpActionExecutorResolver), nameof(Convert), Type.EmptyTypes, result);
            }
            else if (typeof(Task).IsAssignableFrom(result.Type))
            {
                result = Expression.Call(typeof(HttpActionExecutorResolver), nameof(Convert), new[] { result.Type.GenericTypeArguments[0] }, result);
            }
            else if (result.Type == typeof(void))
            {
                result = Expression.Block(result, Expression.Call(typeof(Task), nameof(Task.FromResult), new[] { typeof(object) }, Expression.Constant(null)));
            }
            else
            {
                result = Expression.Call(typeof(Task), nameof(Task.FromResult), new[] { typeof(object) }, Expression.Convert(result, typeof(object)));
            }

            if (variables.Any())
            {
                IEnumerable<ParameterExpression> outVariables = variables.ToArray();
                ParameterExpression resultVariable = Expression.Variable(result.Type, "result");
                variables.Add(resultVariable);
                result = Expression.Block(variables, CollectBlockStatements(resultVariable, result, argumentsParameter, outVariables));
            }

            Expression<ExecuteHttpAction> lambda = Expression.Lambda<ExecuteHttpAction>(result, argumentsParameter);
            ExecuteHttpAction compiled = lambda.Compile();
            string source = (string)DebugViewAccessor.Value.GetValue(lambda);
            return new HttpActionExecutionMethod(action, source, compiled);
        }

        private static T CollectParameter<T>(IDictionary<string, object> arguments, string parameterName)
        {
            if (!arguments.TryGetValue(parameterName, out object value))
                throw new InvalidOperationException($"Missing parameter argument: {parameterName}");

            T result = (T)value;
            return result;
        }

        private static async Task<object> Convert<T>(Task<T> task)
        {
            return await task.ConfigureAwait(false);
        }

        private static async Task<object> Convert(Task task)
        {
            await task.ConfigureAwait(false);
            return null;
        }

        private static IEnumerable<Expression> CollectBlockStatements(Expression resultVariable, Expression resultInstance, Expression argumentsParameter, IEnumerable<ParameterExpression> outVariables)
        {
            Expression callAssign = Expression.Assign(resultVariable, resultInstance);
            yield return callAssign;

            foreach (ParameterExpression outVariable in outVariables)
            {
                Expression property = Expression.Property(argumentsParameter, "Item", Expression.Constant(outVariable.Name));
                Expression assign = Expression.Assign(property, Expression.Convert(outVariable, typeof(object)));
                yield return assign;
            };

            yield return resultVariable;
        }

        private static PropertyAccessor BuildDebugViewAccessor()
        {
            PropertyInfo property = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            return PropertyAccessor.Create(property);
        }

        private delegate Task<object> ExecuteHttpAction(IDictionary<string, object> arguments);

        private sealed class HttpActionExecutionMethod : IHttpActionExecutionMethod
        {
            private readonly IHttpActionDescriptor _action;
            private readonly ExecuteHttpAction _compiled;

            public MethodInfo Method => _action.Target;
            public string Source { get; }

            public HttpActionExecutionMethod(IHttpActionDescriptor action, string source, ExecuteHttpAction compiled)
            {
                this._action = action;
                this._compiled = compiled;
                this.Source = source;
            }

            public async Task<object> Execute(IDictionary<string, object> arguments)
            {
                object result = await this._compiled(arguments).ConfigureAwait(false);
                return result;
            }
        }
    }
}