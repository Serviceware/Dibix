using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionSchemaProvider : ValidatingJsonDefinitionReader, ISchemaProvider
    {
        #region Fields
        private const string RootFolderName = "Contracts";
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IDictionary<string, SchemaDefinition> _schemas;
        #endregion

        #region Properties
        protected override string SchemaName => "dibix.contracts.schema";
        #endregion

        #region Constructor
        public ContractDefinitionSchemaProvider(string productName, string areaName, IEnumerable<TaskItem> contracts, IFileSystemProvider fileSystemProvider, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger) : base(fileSystemProvider, logger)
        {
            _productName = productName;
            _areaName = areaName;
            _typeResolver = typeResolver;
            _schemaRegistry = schemaRegistry;
            _schemas = new Dictionary<string, SchemaDefinition>();
            base.Collect(contracts.Select(x => x.GetFullPath()));
        }
        #endregion

        #region ISchemaProvider Members
        public IEnumerable<SchemaDefinition> Collect() => _schemas.Values;
        #endregion

        #region Overrides
        protected override void Read(JObject json)
        {
            SourceLocation sourceInfo = json.GetSourceInfo();
            string relativePath = Path.GetDirectoryName(sourceInfo.Source).Substring(base.FileSystemProvider.CurrentDirectory.Length + 1);
            string[] parts = relativePath.Split(Path.DirectorySeparatorChar);
            if (parts[0] != RootFolderName)
                throw new InvalidOperationException($"Expected contract root folder to be '{RootFolderName}' but got: {parts[0]}");

            bool multipleAreas = _areaName == null;
            if (multipleAreas && parts.Length < 2)
                throw new InvalidOperationException(@"Expected the following folder structure for projects with multiple areas: Contracts\Area\*.json
If this is not a project that has multiple areas, please make sure to define the <RootNamespace> tag in the following format: Product.Area");

            string relativeNamespace = String.Join(".", parts.Skip(1));
            NamespacePath currentNamespace = PathUtility.BuildAbsoluteNamespace(_productName, _areaName, LayerName.DomainModel, relativeNamespace);

            ReadContracts(currentNamespace, relativeNamespace, json);
        }
        #endregion

        #region Private Methods
        private void ReadContracts(NamespacePath currentNamespace, string relativeNamespace, JObject contracts)
        {
            foreach (JProperty definitionProperty in contracts.Properties())
            {
                ReadContract(currentNamespace, relativeNamespace, definitionProperty.Name, definitionProperty.Value, definitionProperty.GetSourceInfo());
            }
        }

        private void ReadContract(NamespacePath currentNamespace, string relativeNamespace, string definitionName, JToken value, SourceLocation sourceInfo)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                    ReadObjectContract(currentNamespace, relativeNamespace, definitionName, value, sourceInfo);
                    break;

                case JTokenType.Array:
                    ReadEnumContract(currentNamespace, definitionName, value, sourceInfo);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value.Type), value.Type, null);
            }
        }

        private void ReadObjectContract(NamespacePath currentNamespace, string relativeNamespace, string definitionName, JToken value, SourceLocation sourceInfo)
        {
            IList<ObjectSchemaProperty> properties = new Collection<ObjectSchemaProperty>();
            string wcfNamespace = null;
            foreach (JProperty property in ((JObject)value).Properties())
            {
                if (property.Name == "$wcfNs")
                {
                    wcfNamespace = (string)property.Value;
                }
                else
                {
                    JValue typeNameValue;
                    string typeName;
                    JValue defaultValueJson = default;
                    SerializationBehavior serializationBehavior = default;
                    DateTimeKind dateTimeKind = default;
                    bool isPartOfKey = default;
                    bool isOptional = default;
                    bool isDiscriminator = default;
                    bool isObfuscated = default;
                    bool isRelativeHttpsUrl = default;
                    switch (property.Value.Type)
                    {
                        case JTokenType.Object:
                            JObject propertyInfo = (JObject)property.Value;
                            typeNameValue = (JValue)propertyInfo.Property("type").Value;
                            typeName = (string)typeNameValue;
                            defaultValueJson = (JValue)propertyInfo.Property("default")?.Value;
                            Enum.TryParse((string)propertyInfo.Property("serialize")?.Value, true, out serializationBehavior);
                            Enum.TryParse((string)propertyInfo.Property("kind")?.Value, true, out dateTimeKind);
                            isPartOfKey = (bool?)propertyInfo.Property("isPartOfKey")?.Value ?? default;
                            isOptional = (bool?)propertyInfo.Property("isOptional")?.Value ?? default;
                            isDiscriminator = (bool?)propertyInfo.Property("isDiscriminator")?.Value ?? default;
                            isObfuscated = (bool?)propertyInfo.Property("obfuscated")?.Value ?? default;
                            isRelativeHttpsUrl = (bool?)propertyInfo.Property("isRelativeHttpsUrl")?.Value ?? default;
                            break;

                        case JTokenType.String:
                            typeNameValue = (JValue)property.Value;
                            typeName = (string)typeNameValue;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(property.Type), property.Type, null);
                    }

                    SourceLocation propertyLineInfo = property.GetSourceInfo();
                    Token<string> propertyName = new Token<string>(property.Name, propertyLineInfo);

                    TypeReference ResolveType()
                    {
                        bool isEnumerable = typeName.EndsWith("*", StringComparison.Ordinal);
                        typeName = typeName.TrimEnd('*');

                        SourceLocation location = value.GetSourceInfo();
                        TypeReference type = _typeResolver.ResolveType(typeName, relativeNamespace, location, isEnumerable);
                        return type;
                    }

                    ValueReference ResolveDefaultValue(TypeReference typeReference)
                    {
                        ValueReference defaultValue = null;
                        if (defaultValueJson != null)
                        {
                            defaultValue = JsonValueReferenceParser.Parse(typeReference, defaultValueJson, _schemaRegistry, Logger);
                        }
                        return defaultValue;
                    }

                    ObjectSchemaProperty objectSchemaProperty = new ObjectSchemaProperty(propertyName, ResolveType, ResolveDefaultValue, serializationBehavior, dateTimeKind, isPartOfKey, isOptional, isDiscriminator, isObfuscated, isRelativeHttpsUrl);
                    properties.Add(objectSchemaProperty);
                }
            }
            ObjectSchema contract = new ObjectSchema(currentNamespace.Path, definitionName, SchemaDefinitionSource.Defined, sourceInfo, properties, wcfNamespace);
            CollectContract(contract, sourceInfo);
        }

        private void ReadEnumContract(NamespacePath currentNamespace, string definitionName, JToken definitionValue, SourceLocation sourceInfo)
        {
            EnumSchema contract = new EnumSchema(currentNamespace.Path, definitionName, SchemaDefinitionSource.Defined, sourceInfo);

            ICollection<EnumValue> values = ReadEnumValues(definitionValue).ToArray();
            IDictionary<string, int> actualValues = values.Where(x => x.ActualValue.HasValue).ToDictionary(x => x.Name, x => x.ActualValue.Value);

            foreach (EnumValue value in values)
            {
                bool foundCombinationFlag = false;
                int actualValue = value.ActualValue ?? EnumValueParser.ParseDynamicValue(actualValues, value.StringValue, ref foundCombinationFlag);

                if (foundCombinationFlag)
                {
                    // Currently there is no explicit support to mark an enum as flaggable.
                    // Therefore this is the only case where we implicitly detect it.
                    contract.IsFlaggable = true;
                }

                contract.Members.Add(new EnumSchemaMember(value.Name, actualValue, value.StringValue, contract));
            }

            CollectContract(contract, sourceInfo);
        }

        private void CollectContract(SchemaDefinition definition, SourceLocation sourceInfo)
        {
            string name = definition.FullName;
            if (_schemas.TryGetValue(name, out SchemaDefinition otherContract))
            {
                Logger.LogError($"Ambiguous contract definition: {definition.FullName}", otherContract.Location.Source, otherContract.Location.Line, otherContract.Location.Column);
                Logger.LogError($"Ambiguous contract definition: {definition.FullName}", sourceInfo.Source, sourceInfo.Line, sourceInfo.Column);
                return;
            }

            _schemas.Add(name, definition);
        }

        private static IEnumerable<EnumValue> ReadEnumValues(JToken members)
        {
            for (int i = 0; i < ((JArray)members).Count; i++)
            {
                JToken member = ((JArray)members)[i];
                yield return ReadEnumValue(i, member, member.Type);
            }
        }

        private static EnumValue ReadEnumValue(int index, JToken member, JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Object:
                    JProperty property = ((JObject)member).Properties().Single();
                    return ReadEnumValue(property.Name, (JValue)property.Value, property.Value.Type);

                case JTokenType.String:
                    return EnumValue.ImplicitValue((string)((JValue)member).Value, index);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static EnumValue ReadEnumValue(string name, JValue value, JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Integer: return EnumValue.ExplicitValue(name, value.Value<int>());
                case JTokenType.String: return EnumValue.DynamicValue(name, (string)value.Value);
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        #endregion

        #region Nested types
        private sealed class EnumValue
        {
            public string Name { get; }
            public int? ActualValue { get; }
            public string StringValue { get; }

            private EnumValue(string name, int? actualValue, string stringValue)
            {
                Name = name;
                ActualValue = actualValue;
                StringValue = stringValue;
            }

            public static EnumValue ImplicitValue(string name, int actualValue) => new EnumValue(name, actualValue, null);
            public static EnumValue ExplicitValue(string name, int actualValue) => new EnumValue(name, actualValue, actualValue.ToString(CultureInfo.InvariantCulture));
            public static EnumValue DynamicValue(string name, string stringValue) => new EnumValue(name, default, stringValue);
        }
        #endregion
    }
}