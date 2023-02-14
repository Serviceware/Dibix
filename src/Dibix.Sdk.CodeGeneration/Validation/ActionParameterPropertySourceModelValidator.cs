using System;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterPropertySourceModelValidator : ICodeGenerationModelValidator
    {
        private readonly IActionParameterSourceRegistry _actionParameterSourceRegistry;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;

        public ActionParameterPropertySourceModelValidator(IActionParameterSourceRegistry actionParameterSourceRegistry, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            _actionParameterSourceRegistry = actionParameterSourceRegistry;
            _schemaRegistry = schemaRegistry;
            _logger = logger;
        }

        public bool Validate(CodeGenerationModel model)
        {
            bool result = true;
            foreach (ControllerDefinition controller in model.Controllers)
            {
                foreach (ActionDefinition action in controller.Actions)
                {
                    foreach (ActionParameter parameter in action.Parameters)
                    {
                        if (!ValidateSource(parameter, new ActionParameterInfo(parameter.InternalParameterName, parameter.SourceLocation), parameter.ParameterSource, parentValue: null, action))
                            result = false;
                    }
                }
            }
            return result;
        }

        private bool ValidateSource(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterSource currentValue, ActionParameterPropertySource parentValue, ActionDefinition action)
        {
            if (currentValue is not ActionParameterPropertySource propertySource) 
                return true;

            bool result = ValidatePropertySource(rootParameter, currentParameter, propertySource, parentValue, action);

            foreach (ActionParameterItemSource itemPropertySource in propertySource.ItemSources)
            {
                if (!ValidateSource(rootParameter, new ActionParameterInfo(itemPropertySource.ParameterName, itemPropertySource.Location), itemPropertySource.Source, propertySource, action))
                    result = false;
            }

            return result;
        }

        private bool ValidatePropertySource(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition action)
        {
            if (currentValue.Definition == null) // Unknown source is logged at ControllerDefinitionProvider.ReadPropertySource
                return false;

            if (!_actionParameterSourceRegistry.TryGetValidator(currentValue.Definition, out IActionParameterPropertySourceValidator validator))
            {
                //return true;
                throw new InvalidOperationException($"No validator is registered for source '{currentValue.Definition} ({currentValue.Definition.GetType()})'");
            }

            bool result = validator.Validate(rootParameter, currentParameter, currentValue, parentValue, action, _schemaRegistry, _logger);
            return result;
        }
    }
}