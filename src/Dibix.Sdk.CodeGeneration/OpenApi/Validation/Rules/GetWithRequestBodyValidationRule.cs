using System.Collections.Generic;
using System.Net.Http;
using Microsoft.OpenApi;

namespace Dibix.Sdk.CodeGeneration.OpenApi
{
    internal sealed class GetWithRequestBodyValidationRule : ValidationRuleDescriptor<OpenApiPathItem>
    {
        protected override string ErrorMessage => "GET operations cannot have a requestBody";

        protected override void Validate(IValidationContext context, OpenApiPathItem target)
        {
            if (target.Operations == null)
                return;

            foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in target.Operations)
            {
                bool isGet = operation.Key == HttpMethod.Get;
                bool hasRequestBody = operation.Value.RequestBody != null;
                if (!isGet || !hasRequestBody)
                    continue;

                context.Enter(operation.Key.Method.ToLowerInvariant());
                context.Enter("requestBody");
                base.AddError(context);
                context.Exit();
                context.Exit();
            }
        }
    }
}