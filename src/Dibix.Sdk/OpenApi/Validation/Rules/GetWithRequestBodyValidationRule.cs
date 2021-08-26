using System.Collections.Generic;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Dibix.Sdk.OpenApi
{
    internal sealed class GetWithRequestBodyValidationRule : ValidationRuleDescriptor<OpenApiPathItem>
    {
        protected override string ErrorMessage { get; } = "GET operations cannot have a requestBody";

        protected override void Validate(IValidationContext context, OpenApiPathItem target)
        {
            foreach (KeyValuePair<OperationType, OpenApiOperation> operation in target.Operations)
            {
                bool isGet = operation.Key == OperationType.Get;
                bool hasRequestBody = operation.Value.RequestBody != null;
                if (!isGet || !hasRequestBody) 
                    continue;

                context.Enter(operation.Key.GetDisplayName());
                context.Enter("requestBody");
                base.AddError(context);
                context.Exit();
                context.Exit();
            }
        }
    }
}