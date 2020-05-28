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

            parameters.VisitParameters((name, value, clrType, suggestedDataType) =>
            {
                sb.AppendLine();

                sb.Append("Parameter @").Append(name);
                if (suggestedDataType.HasValue)
                    sb.Append(' ').Append(suggestedDataType.ToString().ToUpperInvariant());

                if (value is StructuredType structuredType)
                {
                    sb.Append(' ').Append(structuredType.TypeName);
                    sb.AppendLine().Append(structuredType.Dump());
                }
                else    
                    sb.Append(": ").Append(value);
            });

            return new DatabaseAccessException(sb.ToString(), commandType, commandText, parameters, innerException);
        }
    }
}