using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DacPacSelectionExpression : SourceSelectionExpression, IDacPacSelectionExpression, ISourceSelection
    {
        #region Fields
        private readonly IExecutionEnvironment _environment;
        private readonly string _packagePath;
        private readonly ICollection<KeyValuePair<string, string>> _procedureNames;
        #endregion

        #region Constructor
        public DacPacSelectionExpression(IExecutionEnvironment environment, string filePath)
        {
            this._environment = environment;
            this._packagePath = new PhysicalFileSystemProvider(environment.GetCurrentDirectory()).GetPhysicalFilePath(null, filePath);
            this._procedureNames = new HashSet<KeyValuePair<string, string>>();
        }
        #endregion

        #region IDacPacSelectionExpression Members
        public IDacPacSelectionExpression SelectProcedure(string procedureName, string displayName)
        {
            this._procedureNames.Add(new KeyValuePair<string, string>(displayName, procedureName));
            return this;
        }
        #endregion

        #region Overrides
        protected override IEnumerable<SqlStatementInfo> CollectStatements()
        {
            TSqlModel model = TSqlModel.LoadFromDacpac(this._packagePath, new ModelLoadOptions());
            return this._procedureNames.Select(x => this.CollectStatement(x.Value, x.Key, model));
        }
        #endregion

        #region Private Methods
        private SqlStatementInfo CollectStatement(string procedureName, string displayName, TSqlModel model)
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

            if (base.Parser is ISqlAnalysisRunner sqlAnalysisRunner)
                sqlAnalysisRunner.IsEnabled = false;

            base.Parser.Read(this._environment, SqlParserSourceKind.String, script, statement);
            return statement;
        }
        #endregion
    }
}