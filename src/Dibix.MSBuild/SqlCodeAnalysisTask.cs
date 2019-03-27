using System;
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
    public sealed class SqlCodeAnalysisTask : Task, ITask
    {
        private static readonly string DependentAssemblyProbingDirectory = Path.GetDirectoryName(typeof(SqlCodeAnalysisTask).Assembly.Location);

        public string ProjectDirectory { get; set; }
        public string ProbingDirectory { get; set; }
        public string[] Inputs { get; set; }

        public override bool Execute()
        {
            Assembly sdkAssembly = SdkAssemblyLoader.Load(this.ProjectDirectory);
            Type adapterType = sdkAssembly.GetType($"{Constants.SdkAdapterNamespace}.{nameof(SqlCodeAnalysisTask)}", true);
            object[] args = 
            {
                this.Inputs
              , base.Log
            };
            AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
            bool result = (bool)adapterType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, null, args);
            AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
            return result;
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var query = from directory in new[] { this.ProbingDirectory,DependentAssemblyProbingDirectory }
                        let path = Path.Combine(directory, $"{new AssemblyName(args.Name).Name}.dll")
                        where File.Exists(path)
                        select Assembly.LoadFrom(path);
            return query.FirstOrDefault();
        }
    }
}
