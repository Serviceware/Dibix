using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Http.Host
{
    public sealed class HostingOptions
    {
        public const string ConfigurationSectionName = "Hosting";

        public string? ExternalHostName { get; set; }
        public string? EnvironmentName { get; set; }
        public string? Extension { get; set; }
        // If the application is hosted below a sub path, i.E. IIS Application /WebSite1
        public string? ApplicationBaseAddress { get; set; }
        // Allow relative URLs to have an additional path prefix. This is useful to test the scenario with a set ApplicationBaseAddress locally
        public string? AdditionalPathPrefix { get; set; }
        public ICollection<string> Packages { get; } = new Collection<string>();
    }
}