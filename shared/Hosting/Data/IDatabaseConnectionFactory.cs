using System.Data.Common;

namespace Dibix.Hosting.Abstractions.Data
{
    internal interface IDatabaseConnectionFactory
    {
        DbConnection Create();
    }
}