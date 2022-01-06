namespace Dibix
{
    internal sealed class EnvironmentParameterSource : ActionParameterSourceDefinition<EnvironmentParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        public override string Name => "ENV";
        public string[] Properties { get; } =
        {
            "CurrentProcessId",
            "MachineName"
        };
    }
}