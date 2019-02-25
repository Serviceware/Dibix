using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public class PhysicalSourceSelection : SourceSelection, ISourceSelection
    {
        #region Fields
        private readonly IExecutionEnvironment _environment;
        private readonly string _projectName;
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly ICollection<VirtualPath> _include;
        private readonly ICollection<VirtualPath> _exclude;
        #endregion

        #region Constructor
        public PhysicalSourceSelection(IExecutionEnvironment environment, string projectName)
        {
            this._environment = environment;
            this._projectName = projectName;
            this._fileSystemProvider = projectName != null ? (IFileSystemProvider)environment : new PhysicalFileSystemProvider(environment.GetCurrentDirectory());
            this._include = new HashSet<VirtualPath>();
            this._exclude = new HashSet<VirtualPath>();
        }
        #endregion

        #region Public Methods
        public void Include(string path) => this._include.Add(path);

        public void Exclude(string path) => this._exclude.Add(path);
        #endregion

        #region Overrides
        protected override IEnumerable<SqlStatementInfo> CollectStatements()
        {
            return this._fileSystemProvider
                       .GetFiles(this._projectName, this._include, this._exclude)
                       .Select(x => ParseStatement(this._environment, x, base.Parser));
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