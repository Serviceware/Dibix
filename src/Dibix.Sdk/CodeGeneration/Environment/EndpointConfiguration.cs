using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EndpointConfiguration
    {
        public string BaseUrl { get; set; } = "http://localhost";
        public ICollection<EndpointParameterSource> ParameterSources { get; }

        public EndpointConfiguration()
        {
            this.ParameterSources = new Collection<EndpointParameterSource>();
        }
        public EndpointConfiguration(string baseUrl) : this()
        {
            this.BaseUrl = baseUrl;
        }
    }
}