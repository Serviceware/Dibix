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
        public void ResolveParameterFromNull(string targetParameterName)
        {
            this.ResolveParameter(targetParameterName, new HttpParameterConstantSource(null));
        }
        public void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyName)
        {
            this.ResolveParameterFromSourceCore(targetParameterName, sourceName, sourcePropertyName, null);
        }
        public void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyName, string converterName)
        {
            this.ResolveParameterFromSourceCore(targetParameterName, sourceName, sourcePropertyName, converterName);
        }

        protected HttpParameterPropertySource ResolveParameterFromSourceCore(string targetParameterName, string sourceName, string sourcePropertyName, string converterName)
        {
            HttpParameterPropertySource source = new HttpParameterPropertySource(sourceName, sourcePropertyName, converterName);
            this.ResolveParameter(targetParameterName, source);
            return source;
        }

        protected void ResolveParameter(string targetParameterName, HttpParameterSource source)
        {
            this.ParameterSources.Add(targetParameterName, source);
        }
    }
}