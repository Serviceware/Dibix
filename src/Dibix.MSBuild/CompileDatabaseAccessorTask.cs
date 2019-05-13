using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dibix.Sdk;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Dibix.MSBuild
{
    public sealed class CompileDatabaseAccessorTask : Task, ITask
    {
        public string SdkPath { get; set; }
        public string SSDTDirectory { get; set; }
        public string ProjectDirectory { get; set; }
        public string Namespace { get; set; }
        public string TargetDirectory { get; set; }
        public string[] Inputs { get; set; }
        public string[] ProbingDirectories { get; set; }
        public bool IsDML { get; set; }

        [Output]
        public string OutputFilePath { get; set; }

        [Output]
        public string[] ReferencePaths { get; set; }

        public override bool Execute()
        {
            Assembly sdkAssembly = SdkAssemblyLoader.Load(this.SdkPath);
            Type adapterType = sdkAssembly.GetType($"{Constants.SdkAdapterNamespace}.{nameof(CompileDatabaseAccessorTask)}", true);
            object[] args = 
            {
                this.ProjectDirectory
              , this.Namespace
              , this.TargetDirectory
              , this.Inputs
              , this.ProbingDirectories
              , this.IsDML
              , base.Log
              , null
              , null
            };

            using (new SSDTAssemblyResolver(this.SSDTDirectory))
            {
                bool result = (bool)adapterType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, null, args);
                this.OutputFilePath = (string)args[7];
                this.ReferencePaths = ((ICollection<string>)args[8]).ToArray();
                return result;
            }
        }
    }
}
