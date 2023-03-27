using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Worker.Host
{
    public sealed class HostingOptions
    {
        public const string ConfigurationSectionName = "Hosting";

        public string? Extension { get; set; }
        public ICollection<string> Workers { get; } = new Collection<string>();
    }
}