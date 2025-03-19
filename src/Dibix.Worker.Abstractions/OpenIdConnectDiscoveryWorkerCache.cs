using System.Collections.Concurrent;
using Dibix.Http.Client.OpenIdConnect;

namespace Dibix.Worker.Abstractions
{
    public static class OpenIdConnectDiscoveryWorkerCache
    {
        private static readonly ConcurrentDictionary<string, OpenIdConnectDiscoveryCache> WorkerInstanceCache = new ConcurrentDictionary<string, OpenIdConnectDiscoveryCache>();

        public static OpenIdConnectDiscoveryCache Create<T>() where T : IWorkerExtension => WorkerInstanceCache.GetOrAdd(typeof(T).FullName!, _ => new OpenIdConnectDiscoveryCache());
    }
}