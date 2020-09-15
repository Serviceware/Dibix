using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Dibix.Sdk.Sql
{
    internal static class SqlCmdParser
    {
        public static string ProcessSqlCmdScript(string script, out ICollection<string> includes)
        {
            IList<string> _includes = new Collection<string>();
            string normalizedScript = Regex.Replace(script, @"^:r (?<include>[^\r\n]+)", x =>
            {
                _includes.Add(x.Groups["include"].Value);
                return null;
            }, RegexOptions.Multiline);
            includes = new Collection<string>(_includes);
            
            return !String.IsNullOrWhiteSpace(normalizedScript) ? normalizedScript : null;
        }
    }
}