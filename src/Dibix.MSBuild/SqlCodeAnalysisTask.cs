using System;
using System.IO;
using System.Reflection;
using Dibix.Sdk;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Dibix.MSBuild
{
    public sealed class SqlCodeAnalysisTask : Task, ITask
    {
        private static readonly string DependentAssemblyProbingDirectory = Path.GetDirectoryName(typeof(SqlCodeAnalysisTask).Assembly.Location);

        public string ProjectDirectory { get; set; }
        public string[] Inputs { get; set; }

        public override bool Execute()
        {
            Assembly sdkAssembly = SdkAssemblyLoader.Load(this.ProjectDirectory);
            Type adapterType = sdkAssembly.GetType($"{Constants.SdkAdapterNamespace}.{nameof(SqlCodeAnalysisTask)}", true);
            object[] args = 
            {
                this.ProjectDirectory
              , this.Inputs
              , base.Log
            };
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            bool result = (bool)adapterType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, null, args);
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            return result;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string path = Path.Combine(DependentAssemblyProbingDirectory, $"{new AssemblyName(args.Name).Name}.dll");
            return File.Exists(path) ? Assembly.LoadFile(path) : null;
        }
    }
}
