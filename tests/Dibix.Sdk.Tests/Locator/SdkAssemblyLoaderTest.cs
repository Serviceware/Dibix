using System;
using System.Reflection;
using Xunit;

namespace Dibix.Sdk.Tests.Locator
{
    public class SdkAssemblyLoaderTest
    {
        [Fact]
        public void TryLoadFromHelplineProject()
        {
            const string startDirectory = @"F:\Helpline\HelplineScrum\Development\Dev\SQL\HelplineData";
            Assembly assembly = SdkAssemblyLoader.LocatePackageRootAndLoad(startDirectory);
        }

        [Fact]
        public void TryLoadFromNugetCacheFolder()
        {
            string startDirectory = Environment.CurrentDirectory;
            Assembly assembly = SdkAssemblyLoader.LocatePackageRootAndLoad(startDirectory);
        }
    }
}
