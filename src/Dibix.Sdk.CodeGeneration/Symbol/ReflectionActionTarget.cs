﻿using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public class ReflectionActionTarget : ActionTarget
    {
        public string AssemblyName { get; }

        public ReflectionActionTarget(string assemblyName, string accessorFullName, string operationName, bool isAsync, bool hasRefParameters, SourceLocation sourceLocation) : base(accessorFullName, operationName, isAsync, sourceLocation)
        {
            this.AssemblyName = assemblyName;
        }
    }
}