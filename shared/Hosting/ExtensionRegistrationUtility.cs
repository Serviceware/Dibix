﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Dibix.Hosting.Abstractions
{
    internal static class ExtensionRegistrationUtility
    {
        public static TExtensionInterface GetExtensionImplementation<TExtensionInterface>(string filePath, string kind, AssemblyLoadContext assemblyLoadContext)
        {
            if (!File.Exists(filePath))
                throw new InvalidOperationException($"{kind} not found: {filePath}");

            Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(filePath);
            Type contractType = typeof(TExtensionInterface);
            Type? implementationType = assembly.GetLoadableTypes().FirstOrDefault(contractType.IsAssignableFrom);
            if (implementationType == null)
            {
#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
                throw new InvalidOperationException($@"{kind} does not contain an entrypoint. Please add an implementation for '{contractType}'.
{assembly}
{assembly.Location}");
#pragma warning restore IL3000 // Avoid accessing Assembly file path when publishing as a single file
            }

            TExtensionInterface instance = (TExtensionInterface)Activator.CreateInstance(implementationType)!;
            return instance;
        }
    }
}