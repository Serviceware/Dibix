using System.Data;

namespace Dibix
{
    public abstract class DbParameterCollector
    {
        protected DbProviderAdapter DbProviderAdapter { get; }

        protected DbParameterCollector(DbProviderAdapter dbProviderAdapter)
        {
            DbProviderAdapter = dbProviderAdapter;
        }

        public abstract void VisitInputParameter(string name, DbType dataType, object value, int? size, bool isOutput, CustomInputType customInputType);

        protected static int? NormalizeParameterSize(int? size, DbType? dbType, bool isOutput)
        {
            if (size.HasValue)
                return size;

            switch (dbType)
            {
                case DbType.String when isOutput:
                case DbType.StringFixedLength when isOutput:
                case DbType.AnsiString when isOutput:
                case DbType.AnsiStringFixedLength when isOutput:
                    return -1;

                default:
                    return null;
            }
        }
    }
}