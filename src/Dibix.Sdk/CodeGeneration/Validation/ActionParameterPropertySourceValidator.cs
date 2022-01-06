namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ActionParameterPropertySourceValidator<TSource> : IActionParameterPropertySourceValidator where TSource : ActionParameterSourceDefinition
    {
        protected abstract TSource Definition { get; }

        public abstract bool Validate(ActionParameterPropertySource value, ActionParameterPropertySource parent, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger);
    }
}