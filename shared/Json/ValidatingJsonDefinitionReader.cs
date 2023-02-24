using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk
{
    internal abstract class ValidatingJsonDefinitionReader
    {
        public bool HasSchemaErrors { get; private set; }
        protected IFileSystemProvider FileSystemProvider { get; }
        protected ILogger Logger { get; }
        protected abstract string SchemaName { get; }

        protected ValidatingJsonDefinitionReader(IFileSystemProvider fileSystemProvider, ILogger logger)
        {
            FileSystemProvider = fileSystemProvider;
            Logger = logger;
        }

        protected void Collect(IEnumerable<string> inputs)
        {
            foreach (string filePath in FileSystemProvider.GetFiles(null, inputs.Select(x => (VirtualPath)x), Array.Empty<VirtualPath>()))
            {
                using (Stream stream = File.OpenRead(filePath))
                {
                    using (TextReader textReader = new StreamReader(stream))
                    {
                        using (JsonReader jsonReader = new JsonTextReader(textReader))
                        {
                            JObject json;
                            try
                            {
                                json = JObject.Load(jsonReader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                                json.SetFileSource(filePath);
                                if (!VerifyDuplicatePropertiesCaseInsensitive(json))
                                    continue;
                            }
                            catch (JsonReaderException jsonReaderException)
                            {
                                Logger.LogError(jsonReaderException.Message, filePath, jsonReaderException.LineNumber, jsonReaderException.LinePosition);
                                continue;
                            }

                            if (!json.IsValid(JsonSchemaDefinition.GetSchema(GetType().Assembly, SchemaName), out IList<ValidationError> errors))
                            {
                                foreach (ValidationError error in errors.Flatten())
                                {
                                    LogError(error.Message, error.Path, error.LineNumber, error.LinePosition, filePath);
                                }
                                HasSchemaErrors = true;
                                continue;
                            }

                            Read(json);
                        }
                    }
                }
            }
        }

        protected abstract void Read(JObject json);

        private void LogError(string message, string path, int line, int column, string filePath)
        {
            string errorMessage = $"{message} ({path})";
            Logger.LogError(errorMessage, filePath, line, column);
        }

        private bool VerifyDuplicatePropertiesCaseInsensitive(JToken node)
        {
            bool success = true;

            switch (node.Type)
            {
                case JTokenType.Object:
                {
                    JObject obj = (JObject)node;

                    // Newtonsoft.Json's DuplicatePropertyNameHandling is case sensitive. But we want to be case insensitive.
                    IEnumerable<JProperty> ambiguousProperties = obj.Properties()
                                                                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                                                                    .Where(x => x.Count() > 1)
                                                                    .SelectMany(x => x);

                    foreach (JProperty ambiguousProperty in ambiguousProperties)
                    {
                        SourceLocation location = ambiguousProperty.GetSourceInfo();
                        string errorMessage = $"Property with the name '{ambiguousProperty.Name}' already exists in the current JSON object. Path '{ambiguousProperty.Path}', line {location.Line}, position {location.Column}.";
                        Logger.LogError(errorMessage, location.Source, location.Line, location.Column);
                        
                        // Do not throw, to report subsequent errors
                        //throw new JsonReaderException(errorMessage, ambiguousProperty.Path, location.Line, location.Column, innerException: null);

                        success = false;
                    }

                    foreach (JProperty child in obj.Properties())
                    {
                        if (success)
                            success = VerifyDuplicatePropertiesCaseInsensitive(child.Value);
                    }

                    break;
                }

                case JTokenType.Array:
                {
                    foreach (JToken child in node.Children())
                    {
                        if (success)
                            success = VerifyDuplicatePropertiesCaseInsensitive(child);
                    }

                    break;
                }
            }

            return success;
        }
    }
}