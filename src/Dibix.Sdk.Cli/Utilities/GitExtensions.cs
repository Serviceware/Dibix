using LibGit2Sharp;

namespace Dibix.Sdk.Cli
{
    internal static class GitExtensions
    {
        public static void RevertFile(this IRepository repository, string filePath)
        {
            repository.CheckoutPaths(committishOrBranchSpec: "HEAD", [filePath], new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
        }
    }
}