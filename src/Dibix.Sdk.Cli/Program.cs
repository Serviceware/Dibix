using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.Cli
{
    internal static class Program
    {
        private const int PropertyIndentation = 2;

        private static void Main(string[] args)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            IDictionary<string, InputProperty> arguments = ParseArguments(args[1]).ToDictionary(x => x.PropertyName);
            ILogger logger = new ConsoleLogger();
            switch (args[0])
            {
                case "SqlCodeAnalysisTask":
                    SqlCodeAnalysisTask.Execute
                    (
                        namingConventionPrefix: arguments["NamingConventionPrefix"].GetSingleValue<string>()
                      , databaseSchemaProviderName: arguments["DatabaseSchemaProviderName"].GetSingleValue<string>()
                      , modelCollation: arguments["ModelCollation"].GetSingleValue<string>()
                      , source: arguments["Source"].Values
                      , scriptSource: arguments["ScriptSource"].Values
                      , sqlReferencePath: arguments["SqlReferencePath"].Values
                      , logger: logger
                    );
                    break;

                case "CodeGenerationTask":
                    CodeGenerationTask.Execute
                    (
                        projectDirectory: arguments["ProjectDirectory"].GetSingleValue<string>()
                      , productName: arguments["ProductName"].GetSingleValue<string>()
                      , areaName: arguments["AreaName"].GetSingleValue<string>()
                      , defaultOutputFilePath: arguments["DefaultOutputFilePath"].GetSingleValue<string>()
                      , clientOutputFilePath: arguments["ClientOutputFilePath"].GetSingleValue<string>()
                      , source: arguments["Source"].Values
                      , contracts: arguments["Contracts"].Values
                      , endpoints: arguments["Endpoints"].Values
                      , references: arguments["References"].Values
                      , embedStatements: arguments["EmbedStatements"].GetSingleValue<bool>()
                      , databaseSchemaProviderName: arguments["DatabaseSchemaProviderName"].GetSingleValue<string>()
                      , modelCollation: arguments["ModelCollation"].GetSingleValue<string>()
                      , sqlReferencePath: arguments["SqlReferencePath"].Values
                      , logger: logger
                      , additionalAssemblyReferences: out string[] _
                    );
                    break;

                default:
                    throw new InvalidOperationException($"Invalid task: {args[0]}");
            }
        }

        private static IEnumerable<InputProperty> ParseArguments(string inputFile)
        {
            using (Stream stream = File.OpenRead(inputFile))
            {
                using (TextReader reader = new StreamReader(stream))
                {
                    string line;
                    InputProperty currentProperty = null;
                    ICollection<InputProperty> properties = new Collection<InputProperty>();
                    for (int i = 1; (line = reader.ReadLine()) != null; i++)
                    {
                        if (line.Length == 0)
                            continue;

                        int indentation = GetIndentation(line);
                        bool isPropertyKey = indentation == 0;
                        bool isPropertyValue = indentation == PropertyIndentation;
                        bool isPropertyMetadata = indentation == PropertyIndentation * 2;
                        if (isPropertyKey)
                        {
                            currentProperty = new InputProperty(line);
                            properties.Add(currentProperty);
                        }
                        else if (isPropertyValue)
                        {
                            if (currentProperty == null)
                                throw new InvalidOperationException($"Trying to read property value for an uninitialized property ({i}): {line}");

                            currentProperty.Values.Add(new TaskItem(line.Substring(indentation)));
                        }
                        else if (isPropertyMetadata)
                        {
                            if (currentProperty == null)
                                throw new InvalidOperationException($"Trying to read property metadata for an uninitialized property ({i}): {line}");

                            if (!currentProperty.Values.Any())
                                throw new InvalidOperationException($"Trying to read property metadata for an uninitialized property value ({i}): {line}");

                            string[] parts = line.Substring(indentation).Split(new [] { ' ' }, 2);
                            if (parts.Length < 2)
                                throw new InvalidOperationException($"Property metadata not specified in the format \"Key=Value\" ({i}): {line}");

                            currentProperty.Values.Last().Add(parts[0], parts[1]);
                        }
                    }

                    return properties;
                }
            }
        }

        private static int GetIndentation(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == ' ')
                    continue;

                return i;
            }
            return 0;
        }

        private sealed class InputProperty
        {
            public string PropertyName { get; }
            public ICollection<TaskItem> Values { get; }

            public InputProperty(string propertyName)
            {
                this.Values = new Collection<TaskItem>();
                this.PropertyName = propertyName;
            }

            public T GetSingleValue<T>()
            {
                TaskItem item = this.Values.SingleOrDefault();
                if (item == null)
                    return default;

                return (T)Convert.ChangeType(item.ItemSpec, typeof(T));
            }
        }
    }
}