using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DacPacSourceConfiguration : InputSourceConfiguration
    {
        #region Fields
        private readonly string _packagePath;
        private readonly ICollection<KeyValuePair<string, string>> _procedureNames;
        #endregion

        #region Constructor
        public DacPacSourceConfiguration(IFileSystemProvider fileSystemProvider, string packagePath)
        {
            this._packagePath = new PhysicalFileSystemProvider(fileSystemProvider.CurrentDirectory).GetPhysicalFilePath(null, packagePath);
            this._procedureNames = new HashSet<KeyValuePair<string, string>>();
        }
        #endregion

        #region Public Methods
        public void AddStoredProcedure(string procedureName, string displayName) => this._procedureNames.Add(new KeyValuePair<string, string>(displayName, procedureName));
        #endregion

        #region Overrides
        protected override IEnumerable<SqlStatementInfo> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            TSqlModel model = TSqlModel.LoadFromDacpac(this._packagePath, new ModelLoadOptions());
            return this._procedureNames.Select(x => this.CollectStatement(x.Value, x.Key, model, parser, formatter, contractResolver, errorReporter));
        }
        #endregion

        #region Private Methods
        private SqlStatementInfo CollectStatement(string procedureName, string displayName, TSqlModel model, ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            ICollection<string> parts = procedureName.Split('.').Select(x => x.Trim('[', ']')).ToArray();
            TSqlObject element = model.GetObject(ModelSchema.Procedure, new ObjectIdentifier(parts), DacQueryScopes.All);
            Guard.IsNotNull(element, nameof(element), $"The element {procedureName} could not be found in dacpac");

            string script = element.GetScript();
            SqlStatementInfo statement = new SqlStatementInfo
            {
                Name = displayName,
                Source = this._packagePath
            };

            bool result = parser.Read(SqlParserSourceKind.String, script, statement, null, null, formatter, contractResolver, errorReporter);
            return result ? statement : null;
        }
        #endregion
    }
}