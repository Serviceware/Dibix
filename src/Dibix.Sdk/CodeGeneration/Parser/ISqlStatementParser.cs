using System;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        bool Read(SqlParserSourceKind sourceKind, object content, string source, string definitionName, Lazy<TSqlModel> modelAccessor, SqlCoreConfiguration globalConfiguration, ArtifactGenerationConfiguration artifactGenerationConfiguration, bool analyzeAlways, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger, out SqlStatementDefinition definition);
    }
}