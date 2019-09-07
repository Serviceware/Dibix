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
          , "Microsoft.SqlServer.TransactSql.ScriptDom"
          , "Microsoft.SqlServer.Dac.Extensions"
        };

        public string SdkPath { get; set; }
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

                bool result = (bool)adapterType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, null, args);
                this.PostExecute(args);
                return result;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
            }
        }

        protected abstract IEnumerable<object> CollectParameters();

        protected virtual void PostExecute(object[] args) { }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name;
            string directory = null;

            // Load SSDT assemblies from installation, if running from MSBuild
            if (!this.IsIDEBuild && SSDTAssemblies.Contains(assemblyName))
                directory = this.SSDTDirectory;

            // Newtonsoft.Json.Schema is unknown to both devenv and MSBuild
            if (assemblyName == "Newtonsoft.Json.Schema")
                directory = CurrentDirectory;

            // In some occasions, the previous loading of Newtonsoft.Json.Schema,
            // does include loading the dependent assembly Newtonsoft.Json
            if (assemblyName == "Newtonsoft.Json")
                directory = CurrentDirectory;

            if (directory == null)
                return null;

            string path = Path.Combine(directory, $"{assemblyName}.dll");
            return Assembly.LoadFrom(path);
        }
    }
}