using System.Data;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class FormattedSqlStatement
    {
        public string Content { get; set; }
        public CommandType CommandType { get; }

        public FormattedSqlStatement(string content, CommandType commandType)
        {
            this.Content = content;
            this.CommandType = commandType;
        }
    }
}