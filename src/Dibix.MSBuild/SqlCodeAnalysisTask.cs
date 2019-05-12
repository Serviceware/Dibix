using System;
using System.Diagnostics;
using System.Reflection;
using Dibix.Sdk;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Schema;

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
        public string SdkPath { get; set; }
        public string SSDTDirectory { get; set; }
        public string[] Inputs { get; set; }

        static SqlCodeAnalysisTask()
        {
            // Force loading of referenced assemblies that are needed later on
            new[]
            {
                typeof(JSchemaType) // Newtonsoft.Json.Schema
            }.GetHashCode();
        }

        public override bool Execute()
        {
#if DEBUG
            Debugger.Launch();
#endif
            Assembly sdkAssembly = SdkAssemblyLoader.Load(this.SdkPath);
            Type adapterType = sdkAssembly.GetType($"{Constants.SdkAdapterNamespace}.{nameof(SqlCodeAnalysisTask)}", true);
            object[] args = 
            {
                this.Inputs
              , base.Log
            };

            using (new SSDTAssemblyResolver(this.SSDTDirectory))
            {
                bool result = (bool)adapterType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, null, args);
                return result;
            }
        }
    }
}
