using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class ValidationUtility
    {
        public static bool VerifyPathParameterNotUsedInSource(IActionParameterPropertySource currentValue, ActionDefinition actionDefinition, ActionParameterSourceDefinition sourceDefinition, ILogger logger)
        {
            string sourcePropertyName = currentValue.PropertyName;
            if (actionDefinition.PathParameters.ContainsKey(sourcePropertyName))
            {
                SourceLocation location = currentValue.Location;
                int locationColumn = location.Column + sourceDefinition.Name.Length + 1;
                logger.LogError($"The parameter '{sourcePropertyName}' is a path parameter, therefore cannot be read using the '{sourceDefinition.Name}' source", location.Source, location.Line, locationColumn);
                return false;
            }
            return true;
        }
    }
}