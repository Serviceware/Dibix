using System.IO;
using System.Text.RegularExpressions;

namespace Dibix.Sdk.Sql
{
    public static class SqlCmdParser
    {
        public static string ProcessSqlCmdScript(string directory, string script)
        {
            string normalizedScript = Regex.Replace(script, @"^:r (?<include>[^\r\n]+)", x =>
            {
                string path = Path.Combine(directory, x.Groups["include"].Value);
                string content = File.ReadAllText(path);
                return content;
            }, RegexOptions.Multiline);
            return normalizedScript;
        }
    }
}