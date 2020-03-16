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
        protected IErrorReporter ErrorReporter { get; }
        protected abstract string SchemaName { get; }

        protected JsonSchemaDefinitionReader(IFileSystemProvider fileSystemProvider, IErrorReporter errorReporter)
        {
            this.FileSystemProvider = fileSystemProvider;
            this.ErrorReporter = errorReporter;
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
                                    string errorMessage = $"[JSON] {error.Message} ({error.Path})";
                                    this.ErrorReporter.RegisterError(filePath, error.LineNumber, error.LinePosition, error.ErrorType.ToString(), errorMessage);
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