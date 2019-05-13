using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public class PhysicalSourceConfiguration : InputSourceConfiguration, IPhysicalFileSelection
    {
        #region Fields
        private readonly string _projectName;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IContractDefinitionProvider _contractDefinitionProvider;
        private readonly ICollection<VirtualPath> _include;
        private readonly ICollection<VirtualPath> _exclude;
        private IEnumerable<string> _files;
        #endregion

        #region Properties
        public IEnumerable<string> Files => this._files ?? (this._files = this.GetFiles());
        #endregion

        #region Constructor
        public PhysicalSourceConfiguration(IFileSystemProvider fileSystemProvider, string projectName) : this(fileSystemProvider, null, projectName) { }
        public PhysicalSourceConfiguration(IFileSystemProvider fileSystemProvider, IContractDefinitionProvider contractDefinitionProvider, string projectName)
        {
            this._projectName = projectName;
            this._fileSystemProvider = fileSystemProvider;
            this._contractDefinitionProvider = contractDefinitionProvider;
            this._include = new HashSet<VirtualPath>();
            this._exclude = new HashSet<VirtualPath>();
        }
        #endregion

        #region Public Methods
        public void Include(string path) => this._include.Add(path);

        public void Exclude(string path) => this._exclude.Add(path);
        #endregion

        #region Overrides
        protected override IEnumerable<SqlStatementInfo> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolverFacade, IErrorReporter errorReporter)
        {
            return this.Files
                       .Where(x => Path.GetExtension(x) == ".sql")
                       .Select(x => ParseStatement(x, parser, formatter, contractResolverFacade, errorReporter));
        }

        protected override IEnumerable<ContractDefinition> CollectContracts()
        {
            // Not all entry points support this
            if (this._contractDefinitionProvider == null)
                return Enumerable.Empty<ContractDefinition>();

            return this._contractDefinitionProvider.Contracts;
        }
        #endregion

        #region Private Methods
        private IEnumerable<string> GetFiles()
        {
            return this._fileSystemProvider.GetFiles(this._projectName, this._include, this._exclude);
        }

        private static SqlStatementInfo ParseStatement(string filePath, ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolverFacade, IErrorReporter errorReporter)
        {
            SqlStatementInfo statement = new SqlStatementInfo
            {
                Source = filePath,
                Name = Path.GetFileNameWithoutExtension(filePath)
            };
            parser.Read(SqlParserSourceKind.Stream, File.OpenRead(filePath), statement, formatter, contractResolverFacade, errorReporter);
            return statement;
        }
        #endregion
    }
}