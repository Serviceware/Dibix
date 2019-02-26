namespace Dibix.Sdk.CodeGeneration
{
    public interface IPhysicalSourceSelectionExpression : ISourceConfigurationExpression
    {
        IPhysicalSourceSelectionExpression SelectFolder(string virtualFolderPath, params string[] excludedFolders);
        IPhysicalSourceSelectionExpression SelectFile(string virtualFilePath);
    }
}