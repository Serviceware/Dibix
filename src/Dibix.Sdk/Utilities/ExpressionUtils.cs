using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Dibix.Sdk
{
    internal static class ExpressionUtils
    {
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
            Expression enumeratorValue = Expression.Call(enumerable, typeof(IEnumerable<>).MakeGenericType(elementType).GetMethod(nameof(IEnumerable<object>.GetEnumerator)));
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
            Expression moveNextCall = Expression.Call(enumeratorVariable, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext)));
            Expression enumeratorIfCondition = Expression.Equal(moveNextCall, Expression.Constant(true));
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
            //     enumerator.Dispose();
            // }
            Expression tryBlock = Expression.Block(enumeratorAssign, enumeratorLoop);
            Expression @finally = Expression.Call(enumeratorVariable, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)));
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
