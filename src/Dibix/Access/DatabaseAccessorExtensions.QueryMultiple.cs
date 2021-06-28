using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string commandText)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(commandText, CommandType.Text, ParametersVisitor.Empty);
        }
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(commandText, CommandType.Text, parameters);
        }

        // TaskReminder
        public static Task<IMultipleResultReader> QueryMultipleAsync(this IDatabaseAccessor accessor, string commandText, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultipleAsync(commandText, CommandType.Text, ParametersVisitor.Empty, cancellationToken);
        }
    }
}