using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Http.Host
{
    internal sealed class OptionsMonitorSubscriberOptions
    {
        public ICollection<Func<IServiceProvider, IDisposable?>> Subscribers { get; } = new Collection<Func<IServiceProvider, IDisposable?>>();
    }
}