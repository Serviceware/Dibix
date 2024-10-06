using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.Abstractions
{
    public sealed class InputConfiguration
    {
        private const int IndentationSize = 2;
        private readonly IDictionary<string, InputProperty> _arguments;

        public static InputConfiguration Empty { get; } = new InputConfiguration(new Dictionary<string, InputProperty>());

        private InputConfiguration(IDictionary<string, InputProperty> arguments) => this._arguments = arguments;

        public static InputConfiguration Parse(string inputFile)
        {
            IDictionary<string, InputProperty> arguments = ParseArguments(inputFile).ToDictionary(x => x.PropertyName);
            return new InputConfiguration(arguments);
        }

        public T GetSingleValue<T>(string key, bool throwOnInvalidKey = true)
        {
            InputProperty property = this.GetProperty(key, throwOnInvalidKey);
            if (!throwOnInvalidKey && property == null)
                return default;

            TaskItem item = property.Values.SingleOrDefault();
            if (item == null)
                return default;

            return (T)Convert.ChangeType(item.ItemSpec, typeof(T));
        }

        public ICollection<TaskItem> GetItems(string key)
        {
            InputProperty property = this.GetProperty(key, throwOnInvalidKey: true);
            return property.Values;
        }

        private InputProperty GetProperty(string key, bool throwOnInvalidKey)
        {
            if (!this._arguments.TryGetValue(key, out InputProperty property))
            {
                if (!throwOnInvalidKey)
                    return null;

                throw new KeyNotFoundException($"Property not found: {key}");
            }

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

                        (string value, IndentationLevel indentationLevel) = ParseValue(line);
                        CollectArgument(indentationLevel, line, value, currentPosition: i, properties, ref currentProperty);
                    }

                    return properties;
                }
            }
        }

        private static void CollectArgument(IndentationLevel indentationLevel, string line, string value, int currentPosition, ICollection<InputProperty> properties, ref InputProperty currentProperty)
        {
            switch (indentationLevel)
            {
                case IndentationLevel.Property:
                    currentProperty = new InputProperty(line);
                    properties.Add(currentProperty);
                    break;

                case IndentationLevel.Value:
                    if (currentProperty == null)
                        throw new InvalidOperationException($"Trying to read property value for an uninitialized property ({currentPosition}): {line}");

                    currentProperty.Values.Add(new TaskItem(value));
                    break;

                case IndentationLevel.Metadata:
                    if (currentProperty == null)
                        throw new InvalidOperationException($"Trying to read property metadata for an uninitialized property ({currentPosition}): {line}");

                    if (!currentProperty.Values.Any())
                        throw new InvalidOperationException($"Trying to read property metadata for an uninitialized property value ({currentPosition}): {line}");

                    string[] parts = value.Split(new[] { ' ' }, 2);
                    if (parts.Length < 2)
                        throw new InvalidOperationException($"Property metadata not specified in the format \"Key Value\" ({currentPosition}): {line}");

                    currentProperty.Values.Last().Add(parts[0], parts[1]);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(indentationLevel), indentationLevel, null);
            }
        }

        private static (string value, IndentationLevel indentation) ParseValue(string input)
        {
            int currentIndentation = 0;
            for (int i = 0; i < input.Length; i += IndentationSize)
            {
                if (input[i] != ' ')
                    break;

                currentIndentation++;
            }

            string value = input.Substring(currentIndentation * IndentationSize);
            IndentationLevel indentation = (IndentationLevel)currentIndentation;
            return (value, indentation);
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

        private enum IndentationLevel
        {
            Property,
            Value,
            Metadata
        }
    }
}