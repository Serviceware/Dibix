using System.Collections.Generic;
using System.Diagnostics;

namespace Dibix.Sdk.Abstractions
{
    [DebuggerDisplay("{" + nameof(ItemSpec) + "}")]
    public sealed class TaskItem : Dictionary<string, string>
    {
        public string ItemSpec { get; }

        public TaskItem(string itemSpec) => this.ItemSpec = itemSpec;

        public string GetFullPath() => base["FullPath"];
    }
}