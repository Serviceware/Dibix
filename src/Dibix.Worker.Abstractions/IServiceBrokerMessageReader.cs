using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Worker.Abstractions
{
    public interface IServiceBrokerMessageReader
    {
        Task<IEnumerable<TMessage>> ReadMessages<TMessage>(string fullSubscriberName, Func<IDatabaseAccessorFactory, int, CancellationToken, Task<IEnumerable<TMessage>>> handler, CancellationToken cancellationToken);
    }
}