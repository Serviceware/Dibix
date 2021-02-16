using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, CommandType.Text, EmptyParameters.Instance);
        }
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, CommandType.Text, parameters);
        }

        // TaskReminder
        public static Task<IMultipleResultReader> QueryMultipleAsync(this IDatabaseAccessor accessor, string sql, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultipleAsync(sql, CommandType.Text, EmptyParameters.Instance, cancellationToken);
        }
    }
}