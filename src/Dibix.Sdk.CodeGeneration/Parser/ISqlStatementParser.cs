using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        bool Read(SqlParserSourceKind sourceKind, object content, string source, string definitionName, TSqlModel model, bool isEmbedded, bool limitDdlStatements, bool analyzeAlways, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger, out SqlStatementDefinition definition);
    }
}