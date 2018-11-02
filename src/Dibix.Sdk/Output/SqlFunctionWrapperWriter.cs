using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dibix.Sdk
{
    public class SqlFunctionWrapperWriter : SqlWriter, IWriter
    {
        protected override void Write(StringWriter writer, string projectName, IList<SqlStatementInfo> statements)
        {
            string key = projectName.Split('.')[0].ToLowerInvariant();
            string procedures = String.Join(String.Format("{0}GO{0}", Environment.NewLine), statements.Select(x => this.BuildProcedure(key, x)));
            writer.WriteRaw(procedures);
        }

        private string BuildProcedure(string componentKey, SqlStatementInfo query)
        {
            ICollection<string> lines = base.Format(query.Content)
                                            .Replace(@"\r\n", "\r\n")
                                            .Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                                            .ToArray();

            StringBuilder sb = new StringBuilder();

            string @params = BuildParametersStatement(lines);
            sb.AppendFormat("CREATE PROCEDURE [dbo].[hltest_{0}_{1}]", componentKey, query.Name.ToLowerInvariant())
              .AppendLine()
              .Append(@params);

            if (!String.IsNullOrEmpty(@params))
                sb.AppendLine();

            sb.AppendLine("AS");

            IEnumerable<string> statements = lines.SkipWhile(x => x.StartsWith("--DECLARE"))
                                                  .SkipWhile(String.IsNullOrEmpty)
                                                  .Select(x => String.Concat('\t', x));

            string statement = String.Join(Environment.NewLine, statements);

            sb.AppendLine(statement);
            sb.Append("RETURN");

            return sb.ToString();
        }

        private static string BuildParametersStatement(IEnumerable<string> lines)
        {
            IEnumerable<string> parameters = lines.TakeWhile(x => x.StartsWith("--DECLARE"))
                                                  .Select(x =>
                                                  {
                                                      string[] parts = x.Split(' ');
                                                      string suffix = String.Empty;
                                                      if (parts[2].Split('.')[0].Trim('[', ']') == "dbo")
                                                          suffix = " READONLY";

                                                      return String.Format("{0} {1}{2}", parts[1], parts[2], suffix);
                                                  });

            return String.Join(String.Concat(',', Environment.NewLine), parameters.Select(x => String.Concat('\t', x)));
        }
    }
}