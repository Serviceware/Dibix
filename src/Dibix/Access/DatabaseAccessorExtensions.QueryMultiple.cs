using System.Data;

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
    }
}