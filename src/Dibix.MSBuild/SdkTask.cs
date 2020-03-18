using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dibix.Sdk;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Dibix.MSBuild
{
    /*
      ITaskItem Reference:
        [FullPath, C:\Projects\HelpLineScrum\Development\Dev\SQL\HelplineData\Views\hlsyscasevw.sql]
        [RootDir, C:\]
        [Filename, hlsyscasevw]
        [Extension, .sql]
        [RelativeDir, C:\Projects\HelpLineScrum\Development\Dev\SQL\HelplineData\Views\]
        [Directory, Projects\HelpLineScrum\Development\Dev\SQL\HelplineData\Views\]
        [RecursiveDir, ]
        [Identity, C:\Projects\HelpLineScrum\Development\Dev\SQL\HelplineData\Views\hlsyscasevw.sql]
        [ModifiedTime, 2019-02-21 08:02:11.6820812]
        [CreatedTime, 2016-03-10 11:24:54.8619620]
        [AccessedTime, 2019-02-21 08:02:11.6820812]
        [DefiningProjectFullPath, C:\Projects\HelpLineScrum\Development\Dev\SQL\HelplineData\HelplineData.sqlproj]
        [DefiningProjectDirectory, C:\Projects\HelpLineScrum\Development\Dev\SQL\HelplineData\]
        [DefiningProjectName, HelplineData]
        [DefiningProjectExtension, .sqlproj]
    */
    public abstract class SdkTask : Task, ITask
    {
        private static readonly string[] SSDTAssemblies =
        {
            "Microsoft.Data.Tools.Schema.Sql"
          , "Microsoft.Data.Tools.Schema.Tasks.Sql"
          , "Microsoft.Data.Tools.Schema.Utilities.Sql"
          , "Microsoft.SqlServer.TransactSql.ScriptDom"
          , "Microsoft.SqlServer.Dac.Extensions"
        };
        private static readonly string[] OtherAssemblies =
        {
            "Newtonsoft.Json.Schema"
          , "Newtonsoft.Json" // In some occasions, loading Newtonsoft.Json.Schema does include loading the dependent assembly Newtonsoft.Json
          , "Microsoft.OpenApi"
        };

        public string SdkPath { get; set; }
        public string RuntimePath { get; set; }
        public string SSDTDirectory { get; set; }
        public bool IsIDEBuild { get; set; }
        protected static string CurrentDirectory { get; } = Path.GetDirectoryName(typeof(SdkTask).Assembly.Location);

        public sealed override bool Execute()
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            Assembly sdkAssembly = SdkAssemblyLoader.Load(this.SdkPath);
            Type adapterType = sdkAssembly.GetType($"{Constants.SdkAdapterNamespace}.{this.GetType().Name}", true);
            object[] args = this.CollectParameters().ToArray();
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += this.OnReflectionOnlyAssemblyResolve;

                bool result = (bool)adapterType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, null, args);
                this.PostExecute(args);
                return result;
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= this.OnReflectionOnlyAssemblyResolve;
                AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
            }
        }

        protected abstract IEnumerable<object> CollectParameters();

        protected virtual void PostExecute(object[] args) { }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);
            string name = assemblyName.Name;
            string directory = null;

            // Load SSDT assemblies from installation, if running from MSBuild
            if (!this.IsIDEBuild && SSDTAssemblies.Contains(name))
                directory = this.SSDTDirectory;

            // These are completely unknown to both devenv and MSBuild
            if (OtherAssemblies.Contains(name))
                directory = CurrentDirectory;

            if (directory == null)
                return null;

            string path = Path.Combine(directory, $"{name}.dll");

            if (!File.Exists(path))
                return null;

            assemblyName.CodeBase = path;

            return Assembly.Load(assemblyName);
        }

        private Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);
            if (assemblyName.Name == "Dibix")
                return Assembly.ReflectionOnlyLoadFrom(this.RuntimePath);

            return Assembly.ReflectionOnlyLoad(args.Name);
        }
    }
}