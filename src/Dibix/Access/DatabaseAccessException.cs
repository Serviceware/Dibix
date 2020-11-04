using System;
using System.Data;
using System.Text;

namespace Dibix
{
    public sealed class DatabaseAccessException : Exception
    {
        public CommandType CommandType { get; }
        public string CommandText { get; }
        public IParametersVisitor Parameters { get; }

        private DatabaseAccessException(string message, CommandType commandType, string commandText, IParametersVisitor parameters, Exception innerException) : base($"{innerException.Message}{Environment.NewLine}{message}{Environment.NewLine}", innerException)
        {
            this.CommandType = commandType;
            this.CommandText = commandText;
            this.Parameters = parameters;
        }

        internal static DatabaseAccessException Create(CommandType commandType, string commandText, IParametersVisitor parameters, Exception innerException)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("CommandType: ").Append(commandType).AppendLine()
              .Append("CommandText: ").Append(commandType == CommandType.StoredProcedure ? commandText : "<Dynamic>");

            parameters.VisitInputParameters((name, value, clrType, suggestedDataType, isOutput) =>
            {
                sb.AppendLine();

                sb.Append("Parameter ").Append(name);

                string parameterType = null;
                string parameterDescription = null;

                if (suggestedDataType.HasValue)
                    parameterType = suggestedDataType.ToString().ToUpperInvariant();
                else if (value is StructuredType structuredType)
                {
                    parameterType = structuredType.TypeName;
                    parameterDescription = structuredType.Dump();
                }

                if (parameterType != null)
                {
                    sb.Append('(')
                      .Append(parameterType)
                      .Append(')');
                }
                
                sb.Append(":");

                if (!(value is StructuredType))
                {
                    sb.Append(' ')
                      .Append(value ?? "NULL");
                }

                if (parameterDescription != null)
                {
                    sb.AppendLine()
                      .Append(parameterDescription);
                }
            });

            return new DatabaseAccessException(sb.ToString(), commandType, commandText, parameters, innerException);
        }
    }
}