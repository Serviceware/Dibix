using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public static class ScriptDomFacade
    {
        private static readonly ConcurrentDictionary<string, TSqlFragment> FileCache = new ConcurrentDictionary<string, TSqlFragment>();

        public static TSqlFragment Load(string filePath) => FileCache.GetOrAdd(filePath, x => Load(new StreamReader(x)));

        public static TSqlFragment Parse(string text) => Load(new StringReader(text));

        public static string Generate(TSqlFragment fragment, Action<SqlScriptGenerator> configuration)
        {
            SqlScriptGenerator generator = new Sql140ScriptGenerator();
            configuration?.Invoke(generator);
            generator.GenerateScript(fragment, out string output);
            return output;
        }
        internal static string Generate(TSqlFragment fragment) => Generate(fragment, null);
        
        private static TSqlFragment Load(TextReader reader)
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
    }
}
