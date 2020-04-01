using System;
using System.Collections.Generic;

namespace Dibix.Http
{
    public class HttpParameterSourceSelector : IHttpParameterSourceSelector
    {
        public IDictionary<string, HttpParameterSource> ParameterSources { get; }

        public HttpParameterSourceSelector()
        {
            this.ParameterSources = new Dictionary<string, HttpParameterSource>(StringComparer.OrdinalIgnoreCase);
        }

        public void ResolveParameterFromConstant(string targetParameterName, bool value)
        {
            this.ResolveParameter(targetParameterName, new HttpParameterConstantSource(value));
        }
        public void ResolveParameterFromConstant(string targetParameterName, int value)
        {
            this.ResolveParameter(targetParameterName, new HttpParameterConstantSource(value));
        }
        public void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyName)
        {
            this.ResolveParameterFromSourceCore(targetParameterName, sourceName, sourcePropertyName);
        }

        protected HttpParameterPropertySource ResolveParameterFromSourceCore(string targetParameterName, string sourceName, string sourcePropertyName)
        {
            HttpParameterPropertySource source = new HttpParameterPropertySource(sourceName, sourcePropertyName);
            this.ResolveParameter(targetParameterName, source);
            return source;
        }

        protected void ResolveParameter(string targetParameterName, HttpParameterSource source)
        {
            this.ParameterSources.Add(targetParameterName, source);
        }
    }
}