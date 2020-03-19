namespace Dibix.Sdk.VisualStudio
{
    public interface IPhysicalSourceSelectionExpression : ISourceConfigurationExpression
    {
        IPhysicalSourceSelectionExpression SelectFolder(string virtualFolderPath, params string[] excludedFolders);
        IPhysicalSourceSelectionExpression SelectFile(string virtualFilePath);
    }
}