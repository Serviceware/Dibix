using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk
{
    internal class SourceSelectionExpression : ISourceSelectionExpression, ISourceSelection
    {
        #region Fields
        private const bool IsDefaultRecursive = true;
        private readonly IExecutionEnvironment _environment;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly string _projectName;
        private bool _selectionMade;
        #endregion

        #region Properties
        public ICollection<string> Files { get; }
        public ISqlStatementParser Parser { get; set; }
        #endregion

        #region Constructor
        public SourceSelectionExpression(IExecutionEnvironment environment, string projectName)
        {
            this._environment = environment;
            this._projectName = projectName;
            this._fileSystemProvider = projectName != null ? (IFileSystemProvider)environment : new PhysicalFileSystemProvider(environment.GetCurrentDirectory());
            this.Files = new Collection<string>();
        }
        #endregion

        #region ISourceSelectionExpression Members
        public ISourceSelectionExpression SelectFolder(string virtualFolderPath, params string[] excludedFolders) { return this.SelectFolder(virtualFolderPath, IsDefaultRecursive, excludedFolders); }
        public ISourceSelectionExpression SelectFolder(string virtualFolderPath, bool recursive, params string[] excludedFolders)
        {
            this._selectionMade = true;
            this.Files.AddRange(this._fileSystemProvider.GetFilesInProject(this._projectName, virtualFolderPath, recursive, excludedFolders));
            return this;
        }

        public ISourceSelectionExpression SelectFile(string virtualFilePath)
        {
            this._selectionMade = true;
            this.Files.Add(this._fileSystemProvider.GetPhysicalFilePath(this._projectName, virtualFilePath));
            return this;
        }

        public void SelectParser<TParser>() where TParser : ISqlStatementParser, new() { this.SelectParser<TParser>(null); }
        public void SelectParser<TParser>(Action<ISqlStatementParserConfigurationExpression> configuration) where TParser : ISqlStatementParser, new()
        {
            TParser parser = new TParser();
            SqlStatementParserConfigurationExpression expression = new SqlStatementParserConfigurationExpression(parser);
            configuration?.Invoke(expression);

            this.Parser = parser;
        }
        #endregion

        #region Internal Methods
        internal void Build()
        {
            if (this._selectionMade)
                return;

            this.Files.AddRange(this._fileSystemProvider.GetFilesInProject(this._projectName, null, true, Enumerable.Empty<string>()));
        }
        #endregion
    }
}