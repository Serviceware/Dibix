using System;
using System.Data;
using System.Threading.Tasks;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        // MasterData (InsertGlobalOrganizationViewConfiguration)"
        public static int Execute(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, CommandType.Text, EmptyParameters.Instance);
        }
        public static int Execute(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, commandType, EmptyParameters.Instance);
        }
        // DataImport
        public static int Execute(this IDatabaseAccessor accessor, string sql, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, commandType, configureParameters.Build());
        }
        public static int Execute(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, CommandType.Text, parameters);
        }
        // Configurator (UpdateKnowledgeBaseServiceConfiguration)
        public static Task<int> ExecuteAsync(this IDatabaseAccessor accessor, string sql, IParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteAsync(sql, CommandType.Text, parameters);
        }
    }
}