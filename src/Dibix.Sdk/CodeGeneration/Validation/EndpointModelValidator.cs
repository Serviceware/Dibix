using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Http;

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

                var childRouteGroups = controller.Actions
                                                 .Where(x => x.ChildRoute != null)
                                                 .GroupBy(x => $"{x.Method}#{NormalizeChildRoute(x.ChildRoute)}");

                foreach (IGrouping<string, ActionDefinition> childRouteGroup in childRouteGroups)
                {
                    if (childRouteGroup.Count() <= 1) 
                        continue;

                    foreach (ActionDefinition action in childRouteGroup)
                    {
                        string route = RouteBuilder.BuildRoute(model.AreaName, controller.Name, action.ChildRoute);
                        this._logger.LogError($"Equivalent paths are not allowed: {action.Method.ToString().ToUpperInvariant()} {route}", action.ChildRoute.Source, action.ChildRoute.Line, action.ChildRoute.Column);
                    }
                }
            }

            return isValid;
        }

        private static string NormalizeChildRoute(string childRoute)
        {
            string normalizedChildRoute = Regex.Replace(childRoute, @"\{[^\}]+\}", "");
            return normalizedChildRoute;
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