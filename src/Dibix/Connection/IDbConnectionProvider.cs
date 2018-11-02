using System;
using System.Data.Common;

namespace Dibix
{
    public interface IDbConnectionProvider : IDisposable
    {
        DbConnection GetConnection();
    }
}