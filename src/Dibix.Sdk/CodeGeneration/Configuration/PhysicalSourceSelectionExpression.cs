using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal class PhysicalSourceSelectionExpression : SourceSelectionExpression, IPhysicalSourceSelectionExpression, ISourceSelectionExpression, ISourceSelection
    {
        #region Fields
        private const bool IsDefaultRecursive = true;
        private readonly IExecutionEnvironment _environment;
        private readonly string _projectName;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly ICollection<string> _files;
        private bool _filterApplied;
        #endregion

        #region Constructor
        public PhysicalSourceSelectionExpression(IExecutionEnvironment environment, string projectName)
        {
            this._environment = environment;
            this._projectName = projectName;
            this._fileSystemProvider = projectName != null ? (IFileSystemProvider)environment : new PhysicalFileSystemProvider(environment.GetCurrentDirectory());
            this._files = new Collection<string>();
        }
        #endregion

        #region IPhyiscalSourceSelectionExpression Members
        public IPhysicalSourceSelectionExpression SelectFolder(string virtualFolderPath, params string[] excludedFolders) { return this.SelectFolder(virtualFolderPath, IsDefaultRecursive, excludedFolders); }
        public IPhysicalSourceSelectionExpression SelectFolder(string virtualFolderPath, bool recursive, params string[] excludedFolders)
        {
            this._filterApplied = true;
            this._files.AddRange(this._fileSystemProvider.GetFilesInProject(this._projectName, virtualFolderPath, recursive, excludedFolders));
            return this;
        }

        public IPhysicalSourceSelectionExpression SelectFile(string virtualFilePath)
        {
            this._filterApplied = true;
            this._files.Add(this._fileSystemProvider.GetPhysicalFilePath(this._projectName, virtualFilePath));
            return this;
        }
        #endregion

        #region Overrides
        protected override IEnumerable<SqlStatementInfo> CollectStatements()
        {
            if (!this._filterApplied)
                this._files.AddRange(this._fileSystemProvider.GetFilesInProject(this._projectName, null, true, Enumerable.Empty<string>()));

            return this._files.Select(x => ParseStatement(this._environment, x, base.Parser));
        }
        #endregion

        #region Private Methods
        private static SqlStatementInfo ParseStatement(IExecutionEnvironment environment, string filePath, ISqlStatementParser parser)
        {
            SqlStatementInfo statement = new SqlStatementInfo
            {
                Source = filePath,
                Name = Path.GetFileNameWithoutExtension(filePath)
            };
            parser.Read(environment, SqlParserSourceKind.Stream, File.OpenRead(filePath), statement);
            return statement;
        }
        #endregion
    }
}