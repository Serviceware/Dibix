using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public class PhysicalSourceConfiguration : InputSourceConfiguration, IPhysicalFileSelection
    {
        #region Fields
        private readonly string _projectName;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly ICollection<VirtualPath> _include;
        private readonly ICollection<VirtualPath> _exclude;
        private IEnumerable<string> _files;
        #endregion

        #region Properties
        public IEnumerable<string> Files => this._files ?? (this._files = this.GetFiles());
        #endregion

        #region Constructor
        public PhysicalSourceConfiguration(IFileSystemProvider fileSystemProvider, string projectName, string productName, string areaName)
        {
            this._fileSystemProvider = fileSystemProvider;
            this._projectName = projectName;
            this._productName = productName;
            this._areaName = areaName;
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
            return this.Files
                       .Where(x => Path.GetExtension(x) == ".sql")
                       .Select(x => this.ParseStatement(x, parser, formatter, contractResolver, errorReporter));
        }
        #endregion

        #region Private Methods
        private IEnumerable<string> GetFiles()
        {
            return this._fileSystemProvider.GetFiles(this._projectName, this._include, this._exclude);
        }

        private SqlStatementInfo ParseStatement(string filePath, ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            return SqlStatementParser.ParseStatement(filePath, this._productName, this._areaName, parser, formatter, contractResolver, errorReporter);
        }
        #endregion
    }
}