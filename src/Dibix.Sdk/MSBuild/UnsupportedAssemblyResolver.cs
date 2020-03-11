﻿using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.MSBuild
{
    // Loading foreign assemblies is not supported in CodeGenerationTask.
    // Instead contracts should be defined within the same project in JSON format.
    internal sealed class UnsupportedAssemblyResolver : AssemblyResolver
    {
        protected override bool TryGetAssemblyLocation(string assemblyName, out string path)
        {
            path = null;
            return false;
        }
    }
}