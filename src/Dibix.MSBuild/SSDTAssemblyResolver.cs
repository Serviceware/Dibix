using System;
using System.IO;
using System.Reflection;

namespace Dibix.MSBuild
{
    // Probe DAC assemblies from VS installation to make it more stable in MSBuild
    internal sealed class SSDTAssemblyResolver : IDisposable
    {
        private readonly string _ssdtDirectory;

        public SSDTAssemblyResolver(string ssdtDirectory)
        {
            this._ssdtDirectory = ssdtDirectory;
            AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
        }

        void IDisposable.Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string path = Path.Combine(this._ssdtDirectory, $"{new AssemblyName(args.Name).Name}.dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }
    }
}