using System;

namespace Dibix.Worker.Abstractions
{
    public interface IWorkerExtensionConfigurationBuilder : IWorkerConfigurationBuilder<IWorkerExtensionConfigurationBuilder>
    {
        IWorkerExtensionConfigurationBuilder RegisterHttpClient(string name);
        IWorkerExtensionConfigurationBuilder RegisterHttpClient(string name, Action<IWorkerHttpClientConfigurationBuilder> configure);
    }
}