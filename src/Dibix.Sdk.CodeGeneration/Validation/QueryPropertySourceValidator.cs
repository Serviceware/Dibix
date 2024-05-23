using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class QueryPropertySourceValidator : StaticActionParameterPropertySourceValidator<QueryParameterSource>
    {
        public QueryPropertySourceValidator(QueryParameterSource definition) : base(definition) { }

        public override bool Validate(ActionParameter rootParameter, ActionParameterInfo currentParameter, IActionParameterPropertySource currentValue, IActionParameterPropertySource parentValue, ActionDefinition actionDefinition, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            bool notInPath = ValidationUtility.VerifyPathParameterNotUsedInSource(currentValue, actionDefinition, Definition, logger);
            return notInPath;
        }
    }
}