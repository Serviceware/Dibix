using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public class SqlConstantsWriter : SqlWriter, IWriter
    {
        protected override void Write(StringWriter writer, string projectName, IList<SqlStatementInfo> statements)
        {
            writer.Write("namespace ")
                  .WriteLineRaw(base.Namespace)
                  .WriteLine("{")
                  .PushIndent()
                  .Write("internal static class ")
                  .WriteLineRaw(base.ClassName)
                  .WriteLine("{")
                  .PushIndent();

            IList<SqlStatementInfo> orderedStatements = statements.OrderBy(x => x.Name).ToArray();
            for (int i = 0; i < orderedStatements.Count; i++)
            {
                SqlStatementInfo statement = orderedStatements[i];
                WriteConstant(writer, statement.Name, base.Format(statement.Content), base.Formatting);

                if (i + 1 < orderedStatements.Count)
                    writer.WriteLine();
            }

            writer.WriteLine()
                  .PopIndent()
                  .WriteLine("}")
                  .PopIndent()
                  .Write("}");
        }

        private static void WriteConstant(StringWriter writer, string name, string content, SqlQueryOutputFormatting formatting)
        {
            writer.Write("public const string ")
                  .WriteRaw(name)
                  .WriteRaw(" = ");

            if (formatting.HasFlag(SqlQueryOutputFormatting.Verbatim))
            {
                writer.WriteRaw('@');
            }

            writer.WriteRaw('"')
                  .WriteRaw(content)
                  .WriteRaw("\";");
        }
    }
}