using System;
using System.IO;
using System.Linq;
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
        public string ProbingDirectory { get; set; }
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
