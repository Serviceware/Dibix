using System.Data;

namespace Dibix.Worker.Abstractions
{
    public interface IParameterCollectionContext
    {
        IParameterCollectionContext AddParameter(string parameterName, DbType parameterType, object value, int? size = null);
    }
}