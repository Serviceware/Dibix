using System;
using System.Collections.Generic;
using System.IO;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class NoOpParser : ISqlStatementParser
    {
        private static readonly IDictionary<SqlParserSourceKind, Func<object, string>> SourceReaders = new Dictionary<SqlParserSourceKind, Func<object, string>>
        {
            { SqlParserSourceKind.String, ReadFromString },
            { SqlParserSourceKind.Stream, ReadFromStream }
        };

        public void Read(SqlParserSourceKind sourceKind, object source, SqlStatementInfo target, ISqlStatementFormatter formatter, ITypeLoaderFacade typeLoaderFacade, IErrorReporter errorReporter)
        {
            if (!SourceReaders.TryGetValue(sourceKind, out Func<object, string> reader))
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null);

            target.Content = reader(source);
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