﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Dibix
{
    internal static class ExpressionUtility
    {
        public static Expression CallBaseMethod(Expression instance, Type type, string methodName, params Expression[] parameters)
        {
            MethodInfo method = type.SafeGetMethod(methodName);
            Type[] delegateParameters = method.GetParameters().Select(x => x.ParameterType).ToArray();
            Type delegateType = method.ReturnType == typeof(void) ? Expression.GetActionType(delegateParameters) : Expression.GetDelegateType(delegateParameters);
            Expression functionPointer = Expression.Constant(method.MethodHandle.GetFunctionPointer(), typeof(object));
            Expression createInstanceParameters = Expression.NewArrayInit(typeof(object), Expression.Convert(instance, typeof(object)), functionPointer);
            Expression @delegate = Expression.Call(typeof(Activator), nameof(Activator.CreateInstance), [], Expression.Constant(delegateType, typeof(Type)), createInstanceParameters);
            Expression invocation = Expression.Invoke(Expression.Convert(@delegate, delegateType), parameters);
            return invocation;
        }

        public static void Foreach
        (
            string name
          , Expression enumerable
          , Type elementType
          , Action<IForeachBodyBuilder> bodyBuilder
          , out ParameterExpression enumeratorVariable
          , out Expression enumeratorStatement
        )
        {
            Guard.IsNotNullOrEmpty(name, nameof(name));

            // IEnumerator<T> enumerator;
            enumeratorVariable = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(elementType), $"{name}Enumerator");

            // enumerator = enumerable.GetEnumerator();
            Expression enumeratorValue = Expression.Call(enumerable, typeof(IEnumerable<>).MakeGenericType(elementType).SafeGetMethod(nameof(IEnumerable<object>.GetEnumerator)));
            Expression enumeratorAssign = Expression.Assign(enumeratorVariable, enumeratorValue);

            // T element = enumerator.Current;
            ParameterExpression elementVariable = Expression.Variable(elementType, $"{name}Element");
            Expression elementValue = Expression.Property(enumeratorVariable, nameof(IEnumerator<object>.Current));
            Expression elementAssign = Expression.Assign(elementVariable, elementValue);

            ForeachBodyBuilder bodyBuilderContext = new ForeachBodyBuilder(elementVariable);
            bodyBuilder(bodyBuilderContext);

            // while (enumerator.MoveNext())
            // {
            //     ...
            // }
            Expression moveNextCall = Expression.Call(enumeratorVariable, typeof(IEnumerator).SafeGetMethod(nameof(IEnumerator.MoveNext)));
            Expression enumeratorIfCondition = Expression.IsTrue(moveNextCall);
            Expression enumeratorIfTrue = Expression.Block
            (
                Enumerable.Repeat(elementVariable, 1).Concat(bodyBuilderContext.Variables)
              , Enumerable.Repeat(elementAssign, 1).Concat(bodyBuilderContext.Statements)
            );
            string adjustedName = AdjustName(name);
            LabelTarget enumeratorBreakLabel = Expression.Label($"Break{adjustedName}Enumerator");
            Expression enumeratorIfFalse = Expression.Break(enumeratorBreakLabel);
            Expression enumeratorIfElse = Expression.IfThenElse(enumeratorIfCondition, enumeratorIfTrue, enumeratorIfFalse);
            Expression enumeratorLoop = Expression.Loop(enumeratorIfElse, enumeratorBreakLabel);

            // try
            // {
            //     ...
            // }
            // finally
            // {
            //     if (enumerator != null)
            //         enumerator.Dispose();
            // }
            MethodInfo disposeMethod = typeof(IDisposable).SafeGetMethod(nameof(IDisposable.Dispose));
            Expression disposeEnumerator = Expression.Call(enumeratorVariable, disposeMethod);
            Expression disposeEnumeratorIf = Expression.IfThen(Expression.NotEqual(enumeratorVariable, Expression.Constant(null)), disposeEnumerator);
            Expression tryBlock = Expression.Block(enumeratorAssign, enumeratorLoop);
            Expression @finally = disposeEnumeratorIf;
            Expression tryFinally = Expression.TryFinally(tryBlock, @finally);
            enumeratorStatement = tryFinally;
        }

        private static string AdjustName(string name)
        {
            StringBuilder sb = new StringBuilder(name.Substring(0, 1).ToUpperInvariant());
            if (name.Length > 1)
                sb.Append(name.Substring(1, name.Length - 1));

            return sb.ToString();
        }

        private sealed class ForeachBodyBuilder : IForeachBodyBuilder
        {
            public Expression Element { get; }
            public ICollection<ParameterExpression> Variables { get; }
            public ICollection<Expression> Statements { get; }

            public ForeachBodyBuilder(Expression element)
            {
                this.Element = element;
                this.Variables = new Collection<ParameterExpression>();
                this.Statements = new Collection<Expression>();
            }

            public IForeachBodyBuilder AddAssignStatement(ParameterExpression variable, Expression assign)
            {
                this.Variables.Add(variable);
                return this.AddStatement(assign);
            }

            public IForeachBodyBuilder AddStatement(Expression statement)
            {
                this.Statements.Add(statement);
                return this;
            }
        }
    }

    public interface IForeachBodyBuilder
    {
        Expression Element { get; }

        IForeachBodyBuilder AddStatement(Expression statement);
        IForeachBodyBuilder AddAssignStatement(ParameterExpression variable, Expression assign);
    }
}