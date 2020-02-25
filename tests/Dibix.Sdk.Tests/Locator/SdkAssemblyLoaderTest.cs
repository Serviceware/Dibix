using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Dibix.Sdk.Tests.Locator
{
    public class SdkAssemblyLoaderTest
    {
        [Fact]
        public void TryLoadFromHelplineProject()
        {
            string rootDirectory = Environment.MachineName == "PMCS-TOLO" ? @"D:\Serviceware\Helpline" : @"F:\Helpline";
            string startDirectory = Path.Combine(rootDirectory, @"SQL\HelplineData");
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
