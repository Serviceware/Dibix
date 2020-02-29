using Microsoft.Build.Framework;

namespace Dibix.Sdk.MSBuild
{
    internal static class TaskExtensions
    {
        public static string GetFullPath(this ITaskItem input) => input.GetMetadata("FullPath");
    }
}