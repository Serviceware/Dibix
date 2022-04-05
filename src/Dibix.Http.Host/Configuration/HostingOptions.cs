using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Http.Host
{
    public sealed class HostingOptions
    {
        public const string ConfigurationSectionName = "Hosting";

        public string? Extension { get; set; }
        public string? BaseAddress { get; set; }
        public ICollection<string> Packages { get; } = new Collection<string>();
    }
}