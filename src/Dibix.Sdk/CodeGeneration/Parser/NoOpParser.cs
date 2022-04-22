using System;
using System.Collections.Generic;
using System.Data;
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

        public bool Read(SqlParserSourceKind sourceKind, object content, string source, string definitionName, Lazy<TSqlModel> modelAccessor, string projectName, bool isEmbedded, bool analyzeAlways, string rootNamespace, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger, out SqlStatementDefinition definition)
        {
            if (!SourceReaders.TryGetValue(sourceKind, out Func<object, string> reader))
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null);

            definition = new SqlStatementDefinition(@namespace: null, definitionName, SchemaDefinitionSource.Defined) { Statement = new FormattedSqlStatement(reader(content), CommandType.Text) };
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