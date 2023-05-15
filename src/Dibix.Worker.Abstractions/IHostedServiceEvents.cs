using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Worker.Abstractions
{
    public interface IHostedServiceEvents
    {
        Task OnWorkerStarted(string fullName, CancellationToken cancellationToken);
        Task OnServiceBrokerIterationCompleted(string fullName, CancellationToken cancellationToken);
    }
}