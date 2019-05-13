using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlUserDefinedTypeParser
    {
        public UserDefinedTypeDefinition Parse(string filePath)
        {
            TSqlParser parser = new TSql140Parser(true);
            using (Stream stream = File.OpenRead(filePath))
            {
                using (TextReader reader = new StreamReader(stream))
                {
                    TSqlFragment fragment = parser.Parse(reader, out IList<ParseError> _);
                    
                }
            }

            return null;
        }

        private class UserDefinedTypeVisitor : TSqlFragmentVisitor
        {
            //public override void Visit(user node)
            //{

            //}
        }
    }
}
