﻿namespace Dibix.Sdk.CodeGeneration
{
    public interface IPhysicalSourceSelectionExpression : ISourceSelectionExpression
    {
        IPhysicalSourceSelectionExpression SelectFolder(string virtualFolderPath, params string[] excludedFolders);
        IPhysicalSourceSelectionExpression SelectFolder(string virtualFolderPath, bool recursive, params string[] excludedFolders);
        IPhysicalSourceSelectionExpression SelectFile(string virtualFilePath);
    }
}