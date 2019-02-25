using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal class PhysicalSourceSelectionExpression : SourceSelectionExpression<PhysicalSourceSelection>, IPhysicalSourceSelectionExpression, ISourceSelectionExpression
    {
        #region Constructor
        public PhysicalSourceSelectionExpression(IExecutionEnvironment environment, string projectName) : base(new PhysicalSourceSelection(environment, projectName)) { }
        #endregion

        #region IPhyiscalSourceSelectionExpression Members
        public IPhysicalSourceSelectionExpression SelectFolder(string virtualFolderPath, params string[] excludedFolders)
        {
            base.Configuration.Include(!String.IsNullOrEmpty(virtualFolderPath) ? virtualFolderPath : "./**");
            excludedFolders.Each(base.Configuration.Exclude);
            return this;
        }

        public IPhysicalSourceSelectionExpression SelectFile(string virtualFilePath)
        {
            base.Configuration.Include(virtualFilePath);
            return this;
        }
        #endregion
    }
}