using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PhysicalFileSqlStatementCollector : SqlStatementCollector
    {
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ISqlStatementParser _parser;
        private readonly ISqlStatementFormatter _formatter;
        private readonly IContractResolverFacade _contractResolver;
        private readonly IErrorReporter _errorReporter;
        private readonly IEnumerable<string> _files;

        public PhysicalFileSqlStatementCollector
        (
            string productName
          , string areaName
          , ISqlStatementParser parser
          , ISqlStatementFormatter formatter
          , IContractResolverFacade contractResolver
          , IErrorReporter errorReporter
          , IEnumerable<string> files)
        {
            this._productName = productName;
            this._areaName = areaName;
            this._parser = parser;
            this._formatter = formatter;
            this._contractResolver = contractResolver;
            this._files = files;
            this._errorReporter = errorReporter;
        }

        public override IEnumerable<SqlStatementInfo> CollectStatements()
        {
            return this._files.Select(this.CollectStatement).Where(x => x != null);
        }

        private SqlStatementInfo CollectStatement(string file)
        {
            SqlStatementInfo statement = new SqlStatementInfo
            {
                Source = file,
                Name = Path.GetFileNameWithoutExtension(file)
            };

            bool result = this._parser.Read(SqlParserSourceKind.Stream, File.OpenRead(file), statement, this._productName, this._areaName, this._formatter, this._contractResolver, this._errorReporter);

            return result ? statement : null;
        }
    }
}