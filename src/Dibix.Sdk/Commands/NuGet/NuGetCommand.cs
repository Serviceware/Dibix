using System.CommandLine;

namespace Dibix.Sdk
{
    public sealed class NuGetCommand : Command
    {
        public NuGetCommand() : base("nuget", "Manage Dibix NuGet packages in local package cache for consumer testing.")
        {
            Add(new ClearNuGetPackagesCommand());
        }
    }
}