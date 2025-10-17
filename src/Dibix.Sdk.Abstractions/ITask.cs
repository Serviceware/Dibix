using System.Threading.Tasks;

namespace Dibix.Sdk.Abstractions
{
    public interface ITask
    {
        Task<bool> Execute();
    }
}