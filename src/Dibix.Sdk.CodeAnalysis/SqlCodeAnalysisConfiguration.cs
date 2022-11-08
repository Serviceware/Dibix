using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisConfiguration
    {
        public bool IsEmbedded { get; set; }
        public bool LimitDdlStatements { get; set; }
        public string StaticCodeAnalysisSucceededFile { get; set; }
        public string ResultsFile { get; set; }
        public string NamingConventionPrefix { get; set; }
        public ICollection<TaskItem> Source { get; } = new Collection<TaskItem>();
        public ICollection<TaskItem> ScriptSource { get; } = new Collection<TaskItem>();
    }
}