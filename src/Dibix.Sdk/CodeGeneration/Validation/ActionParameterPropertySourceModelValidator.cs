using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterPropertySourceModelValidator : ICodeGenerationModelValidator
    {
        private readonly IActionParameterSourceRegistry _actionParameterSourceRegistry;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;

        public ActionParameterPropertySourceModelValidator(IActionParameterSourceRegistry actionParameterSourceRegistry, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            this._actionParameterSourceRegistry = actionParameterSourceRegistry;
            this._schemaRegistry = schemaRegistry;
            this._logger = logger;
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
                        if (!ValidateSource(parameter, new ActionParameterInfo(parameter.InternalParameterName, parameter.FilePath, parameter.Line, parameter.Column), parameter.Source, parentValue: null, action))
                            result = false;
                    }
                }
            }
            return result;
        }

        private bool ValidateSource(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterSource currentValue, ActionParameterPropertySource parentValue, ActionDefinition action)
        {
            if (!(currentValue is ActionParameterPropertySource propertySource)) 
                return true;

            bool result = this.ValidatePropertySource(rootParameter, currentParameter, propertySource, parentValue, action);

            foreach (ActionParameterItemSource itemPropertySource in propertySource.ItemSources)
            {
                if (!this.ValidateSource(rootParameter, new ActionParameterInfo(itemPropertySource.ParameterName, itemPropertySource.FilePath, itemPropertySource.Line, itemPropertySource.Column), itemPropertySource.Source, propertySource, action))
                    result = false;
            }

            return result;
        }

        private bool ValidatePropertySource(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition action)
        {
            if (currentValue.Definition == null) // Unknown source is logged at ControllerDefinitionProvider.ReadPropertySource
                return false;

            if (!this._actionParameterSourceRegistry.TryGetValidator(currentValue.Definition, out IActionParameterPropertySourceValidator validator))
            {
                //return true;
                throw new InvalidOperationException($"No validator is registered for source '{currentValue.Definition} ({currentValue.Definition.GetType()})'");
            }

            bool result = validator.Validate(rootParameter, currentParameter, currentValue, parentValue, action, this._schemaRegistry, this._logger);
            return result;
        }
    }
}