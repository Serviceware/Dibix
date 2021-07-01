using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class PhysicalSourceConfiguration : InputSourceConfiguration
    {
        #region Fields
        private readonly string _projectName;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly ICollection<VirtualPath> _include;
        private readonly ICollection<VirtualPath> _exclude;
        #endregion

        #region Constructor
        public PhysicalSourceConfiguration(IFileSystemProvider fileSystemProvider, string projectName)
        {
            this._fileSystemProvider = fileSystemProvider;
            this._projectName = projectName;
            this._include = new HashSet<VirtualPath>();
            this._exclude = new HashSet<VirtualPath>();
        }
        #endregion

        #region Public Methods
        public void Include(string path) => this._include.Add(path);

        public void Exclude(string path) => this._exclude.Add(path);
        #endregion

        #region Overrides
        protected override IEnumerable<SqlStatementDescriptor> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            IEnumerable<string> files = this._fileSystemProvider
                                            .GetFiles(this._projectName, this._include, this._exclude)
                                            .Where(x => Path.GetExtension(x) == ".sql");

            SqlStatementCollector statementCollector = new PhysicalFileSqlStatementCollector
            (
                projectName: this._projectName
              , isEmbedded: true
              , analyzeAlways: false
              , productName: null
              , areaName: null
              , parser: parser
              , formatter: formatter
              , typeResolver: typeResolver
              , schemaRegistry: schemaRegistry
              , logger: logger
              , files: files
              , modelAccessor: new Lazy<TSqlModel>(() => PublicSqlDataSchemaModelLoader.Load(this._projectName, "Microsoft.Data.Tools.Schema.Sql.Sql120DatabaseSchemaProvider", "1033, CI", Enumerable.Empty<TaskItem>(), new TaskItem[0], logger))
            );
            return statementCollector.CollectStatements();
        }
        #endregion
    }
}