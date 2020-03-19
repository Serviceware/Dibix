using System;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class PhysicalSourceConfigurationExpression : SourceConfigurationExpression<PhysicalSourceConfiguration>, IPhysicalSourceSelectionExpression, ISourceConfigurationExpression
    {
        #region Constructor
        public PhysicalSourceConfigurationExpression(PhysicalSourceConfiguration configuration) : base(configuration) { }
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