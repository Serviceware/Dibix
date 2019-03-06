using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public class PhysicalSourceConfiguration : InputSourceConfiguration
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
            this._projectName = projectName;
            this._fileSystemProvider = fileSystemProvider;
            this._include = new HashSet<VirtualPath>();
            this._exclude = new HashSet<VirtualPath>();
        }
        #endregion

        #region Public Methods
        public void Include(string path) => this._include.Add(path);

        public void Exclude(string path) => this._exclude.Add(path);
        #endregion

        #region Overrides
        protected override IEnumerable<SqlStatementInfo> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter, ITypeLoaderFacade typeLoaderFacade, IErrorReporter errorReporter)
        {
            return this._fileSystemProvider
                       .GetFiles(this._projectName, this._include, this._exclude)
                       .Select(x => ParseStatement(x, parser, formatter, typeLoaderFacade, errorReporter));
        }
        #endregion

        #region Private Methods
        private static SqlStatementInfo ParseStatement(string filePath, ISqlStatementParser parser, ISqlStatementFormatter formatter, ITypeLoaderFacade typeLoaderFacade, IErrorReporter errorReporter)
        {
            SqlStatementInfo statement = new SqlStatementInfo
            {
                Source = filePath,
                Name = Path.GetFileNameWithoutExtension(filePath)
            };
            parser.Read(SqlParserSourceKind.Stream, File.OpenRead(filePath), statement, formatter, typeLoaderFacade, errorReporter);
            return statement;
        }
        #endregion
    }
}