using System;

namespace Dibix.Http.Server
{
    public interface IHttpActionBuilderBase : IHttpParameterSourceSelector
    {
        void ResolveParameterFromBody(string targetParameterName, string bodyConverterName);
        void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyName, Action<IHttpParameterSourceSelector> itemSources);
    }
}