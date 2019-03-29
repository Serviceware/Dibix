using System;
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
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, commandType, EmptyParameters.Instance);
        }
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, CommandType.Text, configureParameters.Build());
        }
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, commandType, configureParameters.Build());
        }
        public static IMultipleResultReader QueryMultiple(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.QueryMultiple(sql, CommandType.Text, parameters);
        }
    }
}