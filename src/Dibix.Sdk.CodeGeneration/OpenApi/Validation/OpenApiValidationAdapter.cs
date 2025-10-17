using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.OpenApi;

namespace Dibix.Sdk.CodeGeneration.OpenApi
{
    /// <remarks>
    /// Unfortunately the default implementation <see cref="OpenApiValidator"/>
    /// is missing a lot of the overrides from <see cref="OpenApiVisitorBase"/>.
    /// Therefore, we enable them step by step as they are required by validation rules, with the help of a little reflection.
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

        private sealed class OpenApiValidator : Microsoft.OpenApi.OpenApiValidator
        {
            private static readonly Action<Microsoft.OpenApi.OpenApiValidator, object, Type> ValidateAction = CompileValidateAction();

            public OpenApiValidator(ValidationRuleSet ruleSet) : base(ruleSet) { }

            public override void Visit(IOpenApiPathItem item) => Validate(item);

            private void Validate<T>(T item) => this.Validate(item, typeof(T));
            private void Validate(object item, Type type) => ValidateAction(this, item, type);

            private static Action<Microsoft.OpenApi.OpenApiValidator, object, Type> CompileValidateAction()
            {
                Type type = typeof(Microsoft.OpenApi.OpenApiValidator);
                MethodInfo method = type.SafeGetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(object), typeof(Type)]);
                ParameterExpression validatorParameter = Expression.Parameter(type, "validator");
                ParameterExpression itemParameter = Expression.Parameter(typeof(object), "item");
                ParameterExpression typeParameter = Expression.Parameter(typeof(Type), "type");

                Expression call = Expression.Call(validatorParameter, method, itemParameter, typeParameter);
                Expression<Action<Microsoft.OpenApi.OpenApiValidator, object, Type>> lambda = Expression.Lambda<Action<Microsoft.OpenApi.OpenApiValidator, object, Type>>(call, validatorParameter, itemParameter, typeParameter);
                Action<Microsoft.OpenApi.OpenApiValidator, object, Type> compiled = lambda.Compile();
                return compiled;
            }
        }
    }
}
