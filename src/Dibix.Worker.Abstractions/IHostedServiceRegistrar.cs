using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Worker.Abstractions
{
    public interface IHostedServiceRegistrar
    {
        Task RegisterHostedService(string fullName, CancellationToken cancellationToken);
    }
}