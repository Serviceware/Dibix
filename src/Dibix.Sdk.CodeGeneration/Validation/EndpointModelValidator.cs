using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Http;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EndpointModelValidator : ICodeGenerationModelValidator
    {
        private static readonly ICollection<string> HttpMethods = new HashSet<string>(Enum.GetNames(typeof(ActionMethod)), StringComparer.OrdinalIgnoreCase);
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;

        public EndpointModelValidator(ISchemaRegistry schemaRegistry, ILogger logger)
        {
            _schemaRegistry = schemaRegistry;
            _logger = logger;
        }

        public bool Validate(CodeGenerationModel model)
        {
            string BuildRoute(ControllerDefinition controller, string childRoute) => RouteBuilder.BuildRoute(model.AreaName, controller.Name, childRoute);

            ICollection<ActionRegistration> actions = model.Controllers
                                                           .SelectMany(x => x.Actions, (x, y) => new ActionRegistration(y.Method.ToString().ToUpperInvariant(), path: BuildRoute(x, y.ChildRoute), key: BuildRoute(x, NormalizeChildRoute(y.ChildRoute)), y))
                                                           .ToArray();

            // Use non-short-circuit operator to collect all compiler errors
            bool isValid = ValidateActions(actions) & ValidateEquivalentPaths(actions) & ValidateDuplicateMethods(actions);
            return isValid;
        }

        private bool ValidateActions(IEnumerable<ActionRegistration> actions)
        {
            bool isValid = true;

            foreach (ActionRegistration action in actions)
            {
                if (!ValidateAction(action.Action))
                    isValid = false;
            }

            return isValid;
        }

        private bool ValidateEquivalentPaths(IEnumerable<ActionRegistration> actions)
        {
            bool isValid = true;

            // Find ambiguous paths:
            // - a/{b}/c
            // - a/{c}/d
            foreach (IGrouping<string, ActionRegistration> pathSegmentGroup in actions.GroupBy(x => x.Key))
            {
                if (pathSegmentGroup.Count() <= 1)
                    continue;

                IGrouping<string, ActionRegistration>[] pathGroups = pathSegmentGroup.GroupBy(x => x.Path).ToArray();

                // If the routes are all equal, that's fine
                // - a/{b}/c
                // - a/{b}/c

                // We are looking for
                // - a/{b}/c
                // - a/{b}/c
                // - a/{c}/d

                if (pathGroups.All(x => x.Count() != 1))
                    continue;

                foreach (IGrouping<string, ActionRegistration> pathGroup in pathGroups)
                {
                    // Log first violation of each match
                    ActionRegistration firstViolation = pathGroup.Count() == 1 ? pathGroup.Single() : pathGroup.First();
                    _logger.LogError($"Equivalent path defined: {firstViolation.Method} {firstViolation.Path}", firstViolation.Action.Location);
                    isValid = false;
                }
            }

            return isValid;
        }

        private bool ValidateDuplicateMethods(IEnumerable<ActionRegistration> actions)
        {
            bool isValid = true;

            foreach (IGrouping<string, ActionRegistration> pathSegmentGroup in actions.GroupBy(x => x.Key))
            {
                if (pathSegmentGroup.Count() <= 1) 
                    continue;
                
                foreach (IGrouping<string, ActionRegistration> methodGroup in pathSegmentGroup.GroupBy(x => x.Method))
                {
                    if (methodGroup.Count() <= 1) 
                        continue;

                    foreach (ActionRegistration pair in methodGroup)
                    {
                        _logger.LogError($"Duplicate method defined within path: {pair.Method} {pair.Path}", pair.Action.Location);
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        private bool ValidateAction(ActionDefinition action)
        {
            bool isValid = ValidateReservedPathSegments(action) && ValidateParameters(action);
            return isValid;
        }

        private bool ValidateParameters(ActionDefinition action)
        {
            bool isValid = true;
            foreach (ActionParameter parameter in action.Parameters)
            {
                bool isParameterValid = ValidateAsyncFileUpload(action, parameter) && ValidateComplexQueryParameter(parameter) && ValidateRequiredNullableQueryParameter(parameter);
                if (!isParameterValid)
                    isValid = false;
            }
            return isValid;
        }

        private bool ValidateReservedPathSegments(ActionDefinition action)
        {
            if (action.ChildRoute == null)
                return true;

            bool isValid = true;

            string[] segments = action.ChildRoute.Value.Split('/');

            int columnOffset = 0;
            foreach (string segment in segments)
            {
                if (HttpMethods.Contains(segment))
                {
                    _logger.LogError($"The path segment '{segment}' is a known HTTP verb, which should be indicated by the action method and is therefore redundant: {action.ChildRoute.Value}", action.ChildRoute.Location.Source, action.ChildRoute.Location.Line, action.ChildRoute.Location.Column + columnOffset);
                    isValid = false;
                }
                columnOffset += segment.Length + 1; // Skip segment and slash
            }

            return isValid;
        }

        // When using the BODY.$RAW property source, the raw body will be passed to as a stream to an SqlParameter.
        // The stream will be only access asynchronously if one of the async ADO.NET methods is used.
        // Otherwise, it will cause this exception in the ASP.NET core host:
        //   Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead
        // Therefore, we need to ensure that accessor methods used within endpoints that accept stream parameters are marked as async.
        // See: https://github.com/dotnet/aspnetcore/issues/7644
        private bool ValidateAsyncFileUpload(ActionDefinition action, ActionParameter parameter)
        {
            if (action.Target.IsAsync)
                return true;

            if (parameter.ParameterSource is not ActionParameterPropertySource { Definition: BodyParameterSource, PropertyName: BodyParameterSource.RawPropertyName } propertySource)
                return true;

            _logger.LogError($"The parameter '{parameter.InternalParameterName}' accepts a stream, therefore the accessor must be marked with @Async", propertySource.Location);
            return false;
        }

        private bool ValidateComplexQueryParameter(ActionParameter parameter)
        {
            if (parameter.ParameterLocation != ActionParameterLocation.Query)
                return true;

            if (!parameter.Type.IsUserDefinedType(_schemaRegistry, out UserDefinedTypeSchema userDefinedTypeSchema))
                return true;

            if (userDefinedTypeSchema.Properties.Count <= 1)
                return true;

            _logger.LogError($"Deep object query parameters are not supported: {parameter.InternalParameterName}", parameter.SourceLocation);
            return false;
        }

        private bool ValidateRequiredNullableQueryParameter(ActionParameter parameter)
        {
            if (parameter.ParameterLocation != ActionParameterLocation.Query)
                return true;

            if (!parameter.IsRequired)
                return true;

            if (!parameter.Type.IsNullable)
                return true;

            // See:
            // https://github.com/dotnet/aspnetcore/issues/45500
            // https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2009
            _logger.LogError($"Required nullable query parameters are not supported: '{parameter.InternalParameterName}'. Either make the parameter optional by providing a default value, add it to the path/body or make it non-nullable.", parameter.SourceLocation);
            return false;
        }

        private static string NormalizeChildRoute(string childRoute)
        {
            if (childRoute == null)
                return null;

            string normalizedChildRoute = Regex.Replace(childRoute, @"\{[^\}]+\}", "{}");
            return normalizedChildRoute;
        }

        private readonly struct ActionRegistration
        {
            public string Method { get; }
            public string Path { get; }
            public string Key { get; }
            public ActionDefinition Action { get; }

            public ActionRegistration(string method, string path, string key, ActionDefinition action)
            {
                Method = method;
                Path = path;
                Key = key;
                Action = action;
            }
        }
    }
}