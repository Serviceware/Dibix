namespace Dibix
{
    internal sealed class RequestParameterSource : ActionParameterSourceDefinition<RequestParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        public override string Name => "REQUEST";
        public string[] Properties { get; } =
        {
            "Language",
            "Languages"
        };
    }
}