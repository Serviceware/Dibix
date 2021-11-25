namespace Dibix.Sdk.CodeGeneration
{
    internal static class EndpointConfigurationExtensions
    {
        public static void AppendBuiltInParameterSources(this EndpointConfiguration configuration)
        {
            configuration.ParameterSources.Add(new EndpointParameterSource("BODY", isDynamic: true));
            configuration.ParameterSources.Add(new EndpointParameterSource("ENV")
            {
                Properties =
                {
                    "CurrentProcessId",
                    "MachineName"
                }
            });
            configuration.ParameterSources.Add(new EndpointParameterSource("HEADER", isDynamic: true));
            configuration.ParameterSources.Add(new EndpointParameterSource("PATH", isDynamic: true));
            configuration.ParameterSources.Add(new EndpointParameterSource("QUERY", isDynamic: true));
            configuration.ParameterSources.Add(new EndpointParameterSource("REQUEST")
            {
                Properties =
                {
                    "Language",
                    "Languages"
                }
            });
        }
    }
}