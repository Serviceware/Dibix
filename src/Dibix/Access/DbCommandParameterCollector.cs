using System;
using System.Data;
using System.Data.Common;

namespace Dibix
{
    internal sealed class DbCommandParameterCollector : DbParameterCollector, IDisposable
    {
        private readonly DbCommand _command;

        public DbCommandParameterCollector(DbCommand command, DbProviderAdapter dbProviderAdapter) : base(dbProviderAdapter)
        {
            _command = command;
        }

        public override void VisitInputParameter(string name, DbType dataType, object value, int? size, bool isOutput, CustomInputType customInputType)
        {
            DbParameter parameter = _command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Direction = isOutput ? ParameterDirection.Output : ParameterDirection.Input;

            int? parameterSize = NormalizeParameterSize(size, dataType, isOutput);
            if (parameterSize != null)
                parameter.Size = parameterSize.Value;

            switch (customInputType)
            {
                case CustomInputType.None:
                {
                    parameter.DbType = dataType;

                    if (value is StructuredType udt)
                        DbProviderAdapter.MapStructuredTypeToParameter(parameter, udt);
                    else
                        parameter.Value = value;

                    break;
                }
                case CustomInputType.Uri:
                    parameter.DbType = DbType.String;
                    parameter.Value = value?.ToString();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(customInputType), customInputType, null);
            }

            _command.Parameters.Add(parameter);
        }

        void IDisposable.Dispose() => _command?.Dispose();
    }
}