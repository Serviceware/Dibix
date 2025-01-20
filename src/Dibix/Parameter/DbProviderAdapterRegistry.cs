using System;
using System.Collections.Generic;
using System.Data.Common;
#if NET
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Dibix
{
    public static class DbProviderAdapterRegistry
    {
        private static readonly IDictionary<Type, Func<DbConnection, DbProviderAdapter>> Registry = new Dictionary<Type, Func<DbConnection, DbProviderAdapter>>
        {   
            [typeof(SqlConnection)] = x => new SqlClientAdapter((SqlConnection)x)
        };

        internal static DbProviderAdapter Get(DbConnection connection)
        {
            if (Registry.TryGetValue(connection.GetType(), out Func<DbConnection, DbProviderAdapter> factory))
                return factory(connection);

            return DefaultDbProviderAdapter.Instance;
        }

        public static void Register<TConnection>(Func<TConnection, DbProviderAdapter<TConnection>> factory) where TConnection : DbConnection
        {
            if (Registry.TryGetValue(typeof(TConnection), out Func<DbConnection, DbProviderAdapter> existingImplementation))
                throw new InvalidOperationException($"Connection type '{typeof(TConnection)}' is already registered with the following implementation: {existingImplementation.Method.ReturnType}");

            Registry[typeof(TConnection)] = x => factory((TConnection)x);
        }
    }
}