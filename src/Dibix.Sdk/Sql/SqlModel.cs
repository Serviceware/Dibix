using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    public sealed class SqlModel
    {
        private readonly TSqlModel _model;

        internal SqlModel(TSqlModel model)
        {
            this._model = model;
        }
    }
}
