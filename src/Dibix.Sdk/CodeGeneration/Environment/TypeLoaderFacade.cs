using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class TypeLoaderFacade : ITypeLoaderFacade
    {
        #region Fields
        private readonly ICollection<ITypeLoader> _typeLoaders;
        #endregion

        #region Constructor
        public TypeLoaderFacade() => this._typeLoaders = new Collection<ITypeLoader>();
        public TypeLoaderFacade(IFileSystemProvider fileSystemProvider, IAssemblyLocator assemblyLocator)
        {
            this._typeLoaders = new Collection<ITypeLoader>
            {
                new JsonSchemaTypeLoader(fileSystemProvider),
                new CoreTypeLoader(),
                new ForeignTypeLoader(assemblyLocator)
            };
        }
        #endregion

        #region ITypeLoaderFacade Members
        public void RegisterTypeLoader(ITypeLoader typeLoader) => this._typeLoaders.Add(typeLoader);

        public TypeInfo LoadType(string typeName, Action<string> errorHandler)
        {
            TypeInfo typeInfo = this._typeLoaders.Select(x => x.LoadType(typeName, errorHandler)).FirstOrDefault(x => x != null);
            if (typeInfo == null)
                errorHandler($"Could not resolve type '{typeName}'");

            return typeInfo;
        }
        #endregion
    }

    internal sealed class JsonSchemaTypeLoader : ITypeLoader
    {
        #region Fields
        private readonly IFileSystemProvider _fileSystemProvider;
        #endregion

        #region Constructor
        public JsonSchemaTypeLoader(IFileSystemProvider fileSystemProvider)
        {
            this._fileSystemProvider = fileSystemProvider;
        }

        static JsonSchemaTypeLoader()
        {
            // Newtonsoft.Json.Schema uses an older Newtonsoft.Json version
            // We need Newtonsoft.Json 12 though, because of JsonLoadSettings.DuplicatePropertyNameHandling
            Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
            {
                AssemblyName requestedAssembly = new AssemblyName(e.Name);
                if (requestedAssembly.Name == "Newtonsoft.Json" && requestedAssembly.Version.Major == 11)
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
                    requestedAssembly.Version = new Version(12, 0, 0, 0);
                    return Assembly.Load(requestedAssembly);
                }
                return null;
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }
        #endregion

        #region ITypeLoader Members
        public TypeInfo LoadType(TypeName typeName, Action<string> errorHandler)
        {
            string input = typeName;
            if (input[0] != '#')
                return null;

            string[] parts = input.Substring(1, input.Length - 2).Split('.');
            if (parts.Length != 2)
                return null;

            string schemaName = parts[0];
            string definitionName = parts[1];
            string schemaPath = this._fileSystemProvider.GetPhysicalFilePath(null, $"Contracts/{schemaName}.json");
            using (Stream stream = File.OpenRead(schemaPath))
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(textReader))
                    {
                        JSchema schema = JSchema.Load(jsonReader);
                        schema.GetHashCode();
                        return null;
                    }
                }
            }
        }
        #endregion
    }
}