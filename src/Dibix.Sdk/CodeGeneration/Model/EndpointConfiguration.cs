namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EndpointConfiguration
    {
        public string BaseUrl { get; set; } = "http://localhost";

        public EndpointConfiguration() { }
        public EndpointConfiguration(string baseUrl) : this()
        {
            this.BaseUrl = baseUrl;
        }
    }
}