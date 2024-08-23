using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal sealed class OptionsMonitorSubscriberService : IHostedService
    {
        private readonly IOptions<OptionsMonitorSubscriberOptions> _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICollection<IDisposable> _disposables;

        public OptionsMonitorSubscriberService(IOptions<OptionsMonitorSubscriberOptions> options, IServiceProvider serviceProvider)
        {
            _options = options;
            _disposables = new Collection<IDisposable>();
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (Func<IServiceProvider, IDisposable?> subscriber in _options.Value.Subscribers)
            {
                IDisposable? disposable = subscriber(_serviceProvider);
                if (disposable != null)
                    _disposables.Add(disposable);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (IDisposable disposable in _disposables)
            {
                disposable.Dispose();
            }

            return Task.CompletedTask;
        }
    }
}