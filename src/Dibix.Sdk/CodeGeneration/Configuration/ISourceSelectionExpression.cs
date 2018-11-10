using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISourceSelectionExpression
    {
        ICollection<string> Files { get; }

        ISourceSelectionExpression SelectFolder(string virtualFolderPath, params string[] excludedFolders);
        ISourceSelectionExpression SelectFolder(string virtualFolderPath, bool recursive, params string[] excludedFolders);
        ISourceSelectionExpression SelectFile(string virtualFilePath);
        void SelectParser<TParser>() where TParser : ISqlStatementParser, new();
        void SelectParser<TParser>(Action<ISqlStatementParserConfigurationExpression> configuration) where TParser : ISqlStatementParser, new();
    }
}