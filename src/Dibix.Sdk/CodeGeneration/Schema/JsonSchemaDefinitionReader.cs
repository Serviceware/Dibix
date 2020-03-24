using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class JsonSchemaDefinitionReader
    {
        public bool HasSchemaErrors { get; private set; }
        protected IFileSystemProvider FileSystemProvider { get; }
        protected ILogger Logger { get; }
        protected abstract string SchemaName { get; }

        protected JsonSchemaDefinitionReader(IFileSystemProvider fileSystemProvider, ILogger logger)
        {
            this.FileSystemProvider = fileSystemProvider;
            this.Logger = logger;
        }

        protected void Collect(IEnumerable<string> inputs)
        {
            foreach (string filePath in this.FileSystemProvider.GetFiles(null, inputs.Select(x => (VirtualPath)x), new VirtualPath[0]))
            {
                using (Stream stream = File.OpenRead(filePath))
                {
                    using (TextReader textReader = new StreamReader(stream))
                    {
                        using (JsonReader jsonReader = new JsonTextReader(textReader))
                        {
                            JObject json = JObject.Load(jsonReader/*, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }*/);

                            if (!json.IsValid(JsonSchemaDefinition.GetSchema($"{this.GetType().Namespace}.Schema", this.SchemaName), out IList<ValidationError> errors))
                            {
                                foreach (ValidationError error in errors.Flatten())
                                {
                                    string errorMessage = $"{error.Message} ({error.Path})";
                                    this.Logger.LogError(null, errorMessage, filePath, error.LineNumber, error.LinePosition);
                                }
                                this.HasSchemaErrors = true;
                                continue;
                            }

                            this.Read(filePath, json);
                        }
                    }
                }
            }
        }

        protected abstract void Read(string filePath, JObject json);
    }
}