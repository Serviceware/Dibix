using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public static class ScriptDomFacade
    {
        internal static TSqlFragment Load(string filePath) => Load(new StreamReader(filePath));
        internal static TSqlFragment Load(TextReader reader)
        {
            TSqlParser parser = new TSql140Parser(true);
            using (reader)
            {
                TSqlFragment fragment = parser.Parse(reader, out IList<ParseError> errors);
                if (errors.Any())
                    throw new InvalidOperationException($@"Error parsing SQL statement
{String.Join(Environment.NewLine, errors.Select(x => $"{x.Message} at {x.Line},{x.Column}"))}");

                return fragment;
            }
        }

        public static TSqlFragment Parse(string text) => Load(new StringReader(text));

        internal static string Generate(TSqlFragment fragment) => Generate(fragment, null);
        internal static string Generate(TSqlFragment fragment, Action<SqlScriptGenerator> configuration)
        {
            SqlScriptGenerator generator = new Sql140ScriptGenerator();
            configuration?.Invoke(generator);
            generator.GenerateScript(fragment, out string output);
            return output;
        }
    }
}
