using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        // MasterData (InsertGlobalOrganizationViewConfiguration)"
        public static int Execute(this IDatabaseAccessor accessor, string commandText)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(commandText, CommandType.Text, commandTimeout: null, ParametersVisitor.Empty);
        }
        public static int Execute(this IDatabaseAccessor accessor, string commandText, CommandType commandType)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(commandText, commandType, commandTimeout: null, ParametersVisitor.Empty);
        }
        public static int Execute(this IDatabaseAccessor accessor, string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(commandText, commandType, commandTimeout: null, parameters);
        }
        // DataImport
        public static int Execute(this IDatabaseAccessor accessor, string commandText, CommandType commandType, Action<IParameterBuilder> configureParameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(commandText, commandType, configureParameters.Build());
        }
        public static int Execute(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.Execute(commandText, CommandType.Text, parameters);
        }
        // TaskManagement (ValidateReminders)
        public static Task<int> ExecuteAsync(this IDatabaseAccessor accessor, string commandText, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteAsync(commandText, CommandType.Text, commandTimeout: null, ParametersVisitor.Empty, cancellationToken);
        }
        // Configurator (UpdateKnowledgeBaseServiceConfiguration)
        public static Task<int> ExecuteAsync(this IDatabaseAccessor accessor, string commandText, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteAsync(commandText, CommandType.Text, commandTimeout: null, parameters, cancellationToken);
        }
        public static Task<int> ExecuteAsync(this IDatabaseAccessor accessor, string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(accessor, nameof(accessor));
            return accessor.ExecuteAsync(commandText, commandType, commandTimeout: null, parameters, cancellationToken);
        }
    }
}