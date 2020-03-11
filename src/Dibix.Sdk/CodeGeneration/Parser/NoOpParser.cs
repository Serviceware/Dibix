using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class NoOpParser : ISqlStatementParser
    {
        private static readonly IDictionary<SqlParserSourceKind, Func<object, string>> SourceReaders = new Dictionary<SqlParserSourceKind, Func<object, string>>
        {
            { SqlParserSourceKind.String, ReadFromString },
            { SqlParserSourceKind.Stream, ReadFromStream }
        };

        public bool Read(SqlParserSourceKind sourceKind, object source, Lazy<TSqlModel> modelAccessor, SqlStatementInfo target, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, IErrorReporter errorReporter)
        {
            if (!SourceReaders.TryGetValue(sourceKind, out Func<object, string> reader))
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null);

            target.Content = reader(source);
            return true;
        }

        private static string ReadFromString(object source) => ReadFromTextReader(new StringReader((string)source));

        private static string ReadFromStream(object source) => ReadFromTextReader(new StreamReader((Stream)source));

        private static string ReadFromTextReader(TextReader reader)
        {
            using (reader)
            {
                return reader.ReadToEnd();
            }
        }
    }
}