namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ActionParameterPropertySourceValidator<TSource> : IActionParameterPropertySourceValidator where TSource : ActionParameterSourceDefinition
    {
        protected abstract TSource Definition { get; }

        public abstract bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, ActionParameterPropertySource currentValue, ActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger);
    }
}