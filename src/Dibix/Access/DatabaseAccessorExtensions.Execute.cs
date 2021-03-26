using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        // MasterData (InsertGlobalOrganizationViewConfiguration)"
        public static int Execute(this IDatabaseAccessor accessor, string sql)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, CommandType.Text, commandTimeout: null, ParametersVisitor.Empty);
        }
        public static int Execute(this IDatabaseAccessor accessor, string sql, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, commandType, commandTimeout: null, ParametersVisitor.Empty);
        }
        public static int Execute(this IDatabaseAccessor accessor, string sql, CommandType commandType, ParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, commandType, commandTimeout: null, parameters);
        }
        // DataImport
        public static int Execute(this IDatabaseAccessor accessor, string sql, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, commandType, configureParameters.Build());
        }
        public static int Execute(this IDatabaseAccessor accessor, string sql, ParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(sql, CommandType.Text, parameters);
        }
        // TaskManagement (ValidateReminders)
        public static Task<int> ExecuteAsync(this IDatabaseAccessor accessor, string sql, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteAsync(sql, CommandType.Text, commandTimeout: null, ParametersVisitor.Empty, cancellationToken);
        }
        // Configurator (UpdateKnowledgeBaseServiceConfiguration)
        public static Task<int> ExecuteAsync(this IDatabaseAccessor accessor, string sql, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteAsync(sql, CommandType.Text, commandTimeout: null, parameters, cancellationToken);
        }
        public static Task<int> ExecuteAsync(this IDatabaseAccessor accessor, string sql, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteAsync(sql, commandType, commandTimeout: null, parameters, cancellationToken);
        }
    }
}