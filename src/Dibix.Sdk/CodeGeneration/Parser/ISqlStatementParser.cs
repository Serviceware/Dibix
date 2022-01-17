using System;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        bool Read(SqlParserSourceKind sourceKind, object content, string source, string definitionName, Lazy<TSqlModel> modelAccessor, string projectName, bool isEmbedded, bool analyzeAlways, string rootNamspace, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger, out SqlStatementDefinition definition);
    }
}