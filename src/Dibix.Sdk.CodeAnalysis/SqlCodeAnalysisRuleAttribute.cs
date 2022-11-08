using System;

namespace Dibix.Sdk.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SqlCodeAnalysisRuleAttribute : Attribute
    {
        public int Id { get; }
        public bool IsEnabled { get; set; } = true;

        public SqlCodeAnalysisRuleAttribute(int id)
        {
            this.Id = id;
        }
    }
}