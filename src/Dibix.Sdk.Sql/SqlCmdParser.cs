using System.IO;
using System.Text.RegularExpressions;

namespace Dibix.Sdk.Sql
{
    public static class SqlCmdParser
    {
        public static string ProcessSqlCmdScript(string scriptFilePath)
        {
            string script = File.ReadAllText(scriptFilePath);
            string directory = Path.GetDirectoryName(scriptFilePath);
            string normalizedScript = Regex.Replace(script, @"^:r (?<include>[^\r\n]+)", x =>
            {
                string include = Path.Combine(directory, x.Groups["include"].Value);
                string content = ProcessSqlCmdScript(include);
                return content;
            }, RegexOptions.Multiline);
            return normalizedScript;
        }
    }
}