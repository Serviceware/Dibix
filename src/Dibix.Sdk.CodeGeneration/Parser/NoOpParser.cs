using System.Data;
using System.IO;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class NoOpParser : ISqlStatementParser
    {
        public bool Read
        (
            string filePath
          , string definitionName
          , TSqlModel model
          , bool isEmbedded
          , bool limitDdlStatements
          , bool analyzeAlways
          , string productName
          , string areaName
          , ISqlStatementFormatter formatter
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
          , out SqlStatementDefinition definition
        )
        {
            string content = File.ReadAllText(filePath);
            definition = new SqlStatementDefinition(@namespace: null, relativeNamespace: null, definitionName, SchemaDefinitionSource.Defined, new SourceLocation(filePath, line: 0, column: 0)) { Statement = new FormattedSqlStatement(content, CommandType.Text) };
            return true;
        }
    }
}