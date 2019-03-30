using System;
using System.Reflection;
using Dibix.Sdk;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Assembly = System.Reflection.Assembly;

namespace Dibix.MSBuild
{
    public sealed class VerifyDatabaseAccessorTask : Task, ITask
    {
        public string SdkPath { get; set; }
        public string ProjectDirectory { get; set; }
        public string Namespace { get; set; }
        public string[] AssemblyReferences { get; set; }
        public string[] Inputs { get; set; }

        static VerifyDatabaseAccessorTask()
        {
            // Force loading of referenced assemblies that are needed later on
            new[]
            {
                typeof(DuplicatePropertyNameHandling) // Newtonsoft.Json
              , typeof(JSchemaType)                   // Newtonsoft.Json.Schema
              , typeof(EngineVersion)                 // Microsoft.SqlServer.Dac
              , typeof(TSqlModel)                     // Microsoft.SqlServer.Dac.Extensions
              , typeof(TSqlFragment)                  // Microsoft.SqlServer.TransactSql.ScriptDom
            }.GetHashCode();
        }

        public override bool Execute()
        {
            Assembly sdkAssembly = SdkAssemblyLoader.Load(this.SdkPath);
            Type adapterType = sdkAssembly.GetType($"{Constants.SdkAdapterNamespace}.{nameof(VerifyDatabaseAccessorTask)}", true);
            return (bool)adapterType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, null, new object[]
            {
                this.ProjectDirectory
              , this.Namespace
              , this.AssemblyReferences
              , this.Inputs
              , base.Log
            });
        }
    }
}
