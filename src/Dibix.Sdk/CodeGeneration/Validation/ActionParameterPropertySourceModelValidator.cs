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
                        if (!ValidateSource(parameter.Source, parent: null, action))
                            result = false;
                    }
                }
            }
            return result;
        }

        private bool ValidateSource(ActionParameterSource source, ActionParameterPropertySource parent, ActionDefinition action)
        {
            if (!(source is ActionParameterPropertySource propertySource)) 
                return true;

            bool result = this.ValidatePropertySource(propertySource, parent, action);

            foreach (ActionParameterSource itemPropertySource in propertySource.ItemSources.Values)
            {
                if (!this.ValidateSource(itemPropertySource, propertySource, action))
                    result = false;
            }

            return result;
        }

        private bool ValidatePropertySource(ActionParameterPropertySource source, ActionParameterPropertySource parent, ActionDefinition action)
        {
            if (source.Definition == null) // Unknown source is logged at ControllerDefinitionProvider.ReadPropertySource
                return true;

            if (!this._actionParameterSourceRegistry.TryGetValidator(source.Definition, out IActionParameterPropertySourceValidator validator))
            {
                //return true;
                throw new InvalidOperationException($"No validator is registered for source '{source.Definition} ({source.Definition.GetType()})'");
            }

            bool result = validator.Validate(source, parent, action, this._schemaRegistry, this._logger);
            return result;
        }
    }
}