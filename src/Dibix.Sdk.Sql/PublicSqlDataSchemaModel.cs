using System;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    public readonly struct PublicSqlDataSchemaModel : IDisposable
    {
        private readonly object _hostLoader;
        private readonly Action<object> _disposeHostLoader;

        public TSqlModel Model { get; }

        public PublicSqlDataSchemaModel(TSqlModel model, object hostLoader, Action<object> disposeHostLoader)
        {
            _hostLoader = hostLoader;
            _disposeHostLoader = disposeHostLoader;
            Model = model;
        }

        void IDisposable.Dispose()
        {
            Model?.Dispose();
            _disposeHostLoader(_hostLoader);
        }
    }
}