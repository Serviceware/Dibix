using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.Cli
{
    internal class InputConfiguration
    {
        private const int PropertyIndentation = 2;
        private readonly IDictionary<string, InputProperty> _arguments;

        private InputConfiguration(IDictionary<string, InputProperty> arguments) => this._arguments = arguments;

        public static InputConfiguration Parse(string inputFile)
        {
            IDictionary<string, InputProperty> arguments = ParseArguments(inputFile).ToDictionary(x => x.PropertyName);
            return new InputConfiguration(arguments);
        }

        public T GetSingleValue<T>(string key)
        {
            InputProperty property = this.GetProperty(key);
            TaskItem item = property.Values.SingleOrDefault();
            if (item == null)
                return default;

            return (T)Convert.ChangeType(item.ItemSpec, typeof(T));
        }

        public ICollection<TaskItem> GetItems(string key)
        {
            InputProperty property = this.GetProperty(key);
            return property.Values;
        }

        private InputProperty GetProperty(string key)
        {
            if (!this._arguments.TryGetValue(key, out InputProperty property))
                throw new KeyNotFoundException($"Property not found: {key}");

            return property;
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

                            string[] parts = line.Substring(indentation).Split(new[] { ' ' }, 2);
                            if (parts.Length < 2)
                                throw new InvalidOperationException($"Property metadata not specified in the format \"Key Value\" ({i}): {line}");

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
        }
    }
}