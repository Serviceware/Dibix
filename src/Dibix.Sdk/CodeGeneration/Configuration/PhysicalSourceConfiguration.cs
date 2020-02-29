﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public class PhysicalSourceConfiguration : InputSourceConfiguration
    {
        #region Fields
        private readonly string _projectName;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly ICollection<VirtualPath> _include;
        private readonly ICollection<VirtualPath> _exclude;
        private IEnumerable<string> _files;
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
        protected override IEnumerable<SqlStatementInfo> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            IEnumerable<string> files = this._fileSystemProvider
                                            .GetFiles(this._projectName, this._include, this._exclude)
                                            .Where(x => Path.GetExtension(x) == ".sql");

            SqlStatementCollector statementCollector = new PhysicalFileSqlStatementCollector
            (
                productName: null
              , areaName: null
              , parser: parser
              , formatter: formatter
              , contractResolver: contractResolver
              , errorReporter: errorReporter
              , files: files
              , modelAccessor: new Lazy<TSqlModel>(() => throw new NotSupportedException())
            );
            return statementCollector.CollectStatements();
        }
        #endregion
    }
}