using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Microsoft.OpenApi.Validations;

namespace Dibix.Sdk.OpenApi.Validation
{
    /// <remarks>
    /// Unfortunately the default implementation <see cref="OpenApiValidator"/>
    /// is missing a lot of the overrides from <see cref="OpenApiVisitorBase"/>.
    /// Therefore we enable them step by step as they are required by validation rules, with the help of a little reflection.
    /// </remarks>
    internal static class OpenApiValidationAdapter
    {
        public static IEnumerable<OpenApiError> Validate(OpenApiDocument document, ValidationRuleSet ruleSet)
        {
            OpenApiValidator validator = new OpenApiValidator(ruleSet);
            OpenApiWalker walker = new OpenApiWalker(validator);
            walker.Walk(document);
            return validator.Errors;
        }

        private sealed class OpenApiValidator : Microsoft.OpenApi.Validations.OpenApiValidator
        {
            private static readonly Action<Microsoft.OpenApi.Validations.OpenApiValidator, object, Type> ValidateAction = CompileValidateAction();

            public OpenApiValidator(ValidationRuleSet ruleSet) : base(ruleSet) { }

            public override void Visit(OpenApiPathItem item) => this.Validate(item);

            private void Validate<T>(T item) => this.Validate(item, typeof(T));
            private void Validate(object item, Type type) => ValidateAction(this, item, type);

            private static Action<Microsoft.OpenApi.Validations.OpenApiValidator, object, Type> CompileValidateAction()
            {
                Type type = typeof(Microsoft.OpenApi.Validations.OpenApiValidator);
                MethodInfo method = type.GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(object), typeof(Type) }, null);
                if (method == null)
                    throw new InvalidOperationException($"Could not find method 'private void Validate(object, Type)' on type '{type}");

                ParameterExpression validatorParameter = Expression.Parameter(type, "validator");
                ParameterExpression itemParameter = Expression.Parameter(typeof(object), "item");
                ParameterExpression typeParameter = Expression.Parameter(typeof(Type), "type");

                Expression call = Expression.Call(validatorParameter, "Validate", new Type[0], itemParameter, typeParameter);
                Expression<Action<Microsoft.OpenApi.Validations.OpenApiValidator, object, Type>> lambda = Expression.Lambda<Action<Microsoft.OpenApi.Validations.OpenApiValidator, object, Type>>(call, validatorParameter, itemParameter, typeParameter);
                Action<Microsoft.OpenApi.Validations.OpenApiValidator, object, Type> compiled = lambda.Compile();
                return compiled;
            }
        }
    }
}
