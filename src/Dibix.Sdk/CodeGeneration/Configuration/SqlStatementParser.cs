using System.IO;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlStatementParser
    {
        public static SqlStatementInfo ParseStatement(string filePath, string productName, string areaName, ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            SqlStatementInfo statement = new SqlStatementInfo
            {
                Source = filePath,
                Name = Path.GetFileNameWithoutExtension(filePath)
            };

            bool result = parser.Read(SqlParserSourceKind.Stream, File.OpenRead(filePath), statement, productName, areaName, formatter, contractResolver, errorReporter);

            return result ? statement : null;
        }
    }
}