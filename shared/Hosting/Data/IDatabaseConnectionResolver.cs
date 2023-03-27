using System.Data.Common;

namespace Dibix.Hosting.Abstractions.Data
{
    internal interface IDatabaseConnectionResolver
    {
        DbConnection Resolve();
    }
}