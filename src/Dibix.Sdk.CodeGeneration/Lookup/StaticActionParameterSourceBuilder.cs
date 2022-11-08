namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class StaticActionParameterSourceBuilder : ActionParameterSourceBuilder
    {
        private readonly ActionParameterSource _source;

        public StaticActionParameterSourceBuilder(ActionParameterSource source) => _source = source;

        public override ActionParameterSource Build(TypeReference type) => _source;
    }
}