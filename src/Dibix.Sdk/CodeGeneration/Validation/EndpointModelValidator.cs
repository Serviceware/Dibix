using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EndpointModelValidator : ICodeGenerationModelValidator
    {
        private static readonly ICollection<string> HttpMethods = new HashSet<string>(Enum.GetNames(typeof(ActionMethod)), StringComparer.OrdinalIgnoreCase);
        private readonly ILogger _logger;

        public EndpointModelValidator(ILogger logger)
        {
            this._logger = logger;
        }

        public bool Validate(CodeGenerationModel model)
        {
            bool isValid = true;

            foreach (ControllerDefinition controller in model.Controllers)
            {
                foreach (ActionDefinition action in controller.Actions)
                {
                    if (!this.ValidateAction(action))
                        isValid = false;
                }
            }

            return isValid;
        }

        private bool ValidateAction(ActionDefinition action)
        {
            if (action.ChildRoute == null)
                return true;

            bool result = true;

            string[] segments = action.ChildRoute.Value.Split('/');

            int columnOffset = 0;
            foreach (string segment in segments)
            {
                if (HttpMethods.Contains(segment))
                {
                    this._logger.LogError($"The path segment '{segment}' is a known HTTP verb, which should be indicated by the action method and is therefore redundant: {action.ChildRoute.Value}", action.ChildRoute.Source, action.ChildRoute.Line, action.ChildRoute.Column + columnOffset);
                    result = false;
                }
                columnOffset += segment.Length + 1; // Skip segment and slash
            }

            return result;
        }
    }
}