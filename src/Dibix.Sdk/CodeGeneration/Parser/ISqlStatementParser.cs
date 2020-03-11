using System;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        bool Read(SqlParserSourceKind sourceKind, object source, Lazy<TSqlModel> modelAccessor, SqlStatementInfo target, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, IErrorReporter errorReporter);
    }
}