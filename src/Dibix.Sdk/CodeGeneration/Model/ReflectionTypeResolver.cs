using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ReflectionTypeResolver : TypeResolver
    {
        #region Fields
        private readonly AssemblyResolver _assemblyResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;
        #endregion

        #region Constructor
        public ReflectionTypeResolver(AssemblyResolver assemblyResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            this._assemblyResolver = assemblyResolver;
            this._schemaRegistry = schemaRegistry;
            this._logger = logger;
        }
        #endregion

        #region Overrides
        public override TypeReference ResolveType(string input, string relativeNamespace, string source, int line, int column, bool isEnumerable)
        {
            bool isAssemblyQualified = input.IndexOf(',') >= 0;
            return !isAssemblyQualified ? null/*this.TryLocalType(input, source, line, column, isEnumerable)*/ : this.TryForeignType(input, source, line, column, isEnumerable);
        }
        #endregion

        #region Public Methods
        public static TypeReference ResolveForeignType(Type type, string source, int line, int column, bool isNullable, bool isEnumerable, ISchemaRegistry schemaRegistry, ILogger logger, AssemblyResolver assemblyResolver) => ReflectionOnlyTypeInspector.Inspect(() => ResolveType(type, source, line, column, isNullable, isEnumerable, udtName: null, schemaRegistry, logger), assemblyResolver);
        public static TypeReference ResolveType(Type type, string source, int line, int column, string udtName, ISchemaRegistry schemaRegistry, ILogger logger) => ResolveType(type, source, line, column, isNullable: false, isEnumerable: false, udtName, schemaRegistry, logger);
        public static TypeReference ResolveType(Type type, string source, int line, int column, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            string udtName = type.GetUdtName();
            Type underlyingEnumerableType = null;
            Type underlyingNullableType = null;
            if (String.IsNullOrEmpty(udtName))
            {
                underlyingEnumerableType = GetUnderlyingEnumerableType(type);
                underlyingNullableType = Nullable.GetUnderlyingType(underlyingEnumerableType ?? type);
            }

            Type normalizedType = underlyingNullableType ?? underlyingEnumerableType ?? type;
            bool isEnumerable = underlyingEnumerableType != null;
            bool isNullable = underlyingNullableType != null;
            return ResolveType(normalizedType, source, line, column, isNullable, isEnumerable, udtName, schemaRegistry, logger);
        }
        #endregion

        #region Private Methods
        private TypeReference TryLocalType(string input, string source, int line, int column, bool isEnumerable)
        {
            NullableTypeName typeName = input;

            // Try CSharp type name first (string => System.String)
            Type type = typeName.Name.ToClrType();

            if (type == null)
                type = Type.GetType(typeName.Name);

            if (type == null)
                return null;

            return this.ResolveType(type, source, line, column, typeName.IsNullable, isEnumerable);
        }

        private TypeReference TryForeignType(string input, string source, int line, int column, bool isEnumerable)
        {
            try
            {
                string[] parts = input.Split(',');
                if (parts.Length != 2)
                    return null;

                NullableTypeName typeName = parts[0];
                string assemblyName = parts[1];

                if (this._assemblyResolver.TryGetAssembly(assemblyName, out Assembly assembly))
                {
                    Type type = assembly.GetType(typeName.Name, true);
                    return ReflectionOnlyTypeInspector.Inspect(() => this.ResolveType(type, source, line, column, typeName.IsNullable, isEnumerable), this._assemblyResolver);
                }

                this._logger.LogError($"Could not locate assembly: {assemblyName}", source, line, column + parts[0].Length + 1);
                return null;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message, source, line, column);
                return null;
            }
        }

        private TypeReference ResolveType(Type type, string source, int line, int column, bool isNullable, bool isEnumerable) => ResolveType(type, source, line, column, isNullable, isEnumerable, udtName: null, this._schemaRegistry, this._logger);
        private static TypeReference ResolveType(Type type, string source, int line, int column, bool isNullable, bool isEnumerable, string udtName, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (type.IsByRef)
                throw new InvalidOperationException($"By ref types are not supported: {type}");

            if (PrimitiveTypeMap.TryParsePrimitiveType(type, out PrimitiveType dataType))
                return new PrimitiveTypeReference(dataType, isNullable, isEnumerable, source, line, column);

            SchemaTypeReference schemaTypeReference = new SchemaTypeReference($"{type.Namespace}.{type.Name}", isNullable, isEnumerable, source, line, column);
            if (schemaRegistry.IsRegistered(schemaTypeReference.Key))
                return schemaTypeReference;

            SchemaDefinitionSource schemaDefinitionSource = DetermineSchemaDefinitionSource(type);
            if (!String.IsNullOrEmpty(udtName))
            {
                MethodInfo addMethod = type.SafeGetMethod("Add");
                IList<ObjectSchemaProperty> udtProperties = addMethod.GetParameters()
                                                                     .Select(x => new ObjectSchemaProperty(name: new Token<string>(x.Name, source, line, column), ResolveType(x.ParameterType, source, line, column, schemaRegistry, logger)))
                                                                     .ToArray();
                UserDefinedTypeSchema udtSchema = new UserDefinedTypeSchema(type.Namespace, type.Name, schemaDefinitionSource, udtName, udtProperties);
                schemaRegistry.Populate(udtSchema);
            }
            else if (type.IsEnum)
            {
                EnumSchema enumSchema = new EnumSchema(type.Namespace, type.Name, schemaDefinitionSource, isFlaggable: false);

                // Enum.GetValues() => "The requested operation is invalid in the ReflectionOnly context"
                for (int i = 0; i < type.GetFields(BindingFlags.Public | BindingFlags.Static).Length; i++)
                {
                    FieldInfo member = type.GetFields(BindingFlags.Public | BindingFlags.Static)[i];
                    int actualValue = (int)member.GetRawConstantValue();
                    string stringValue = i != actualValue ? actualValue.ToString(CultureInfo.InvariantCulture) : null;
                    enumSchema.Members.Add(new EnumSchemaMember(member.Name, actualValue, stringValue, enumSchema));
                }

                schemaRegistry.Populate(enumSchema);
            }
            else
            {
                IList<ObjectSchemaProperty> objectSchemaProperties = type.GetProperties()
                                                                         .Select(x => CreateProperty(x, source, line, column, schemaRegistry, logger))
                                                                         .ToArray();
                ObjectSchema objectSchema = new ObjectSchema(type.Namespace, type.Name, schemaDefinitionSource, objectSchemaProperties);
                schemaRegistry.Populate(objectSchema);
            }

            return schemaTypeReference;
        }

        private static ObjectSchemaProperty CreateProperty(PropertyInfo property, string source, int line, int column, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            TypeReference typeReference = ResolveType(property.PropertyType, source, line, column, schemaRegistry, logger);
            if (property.IsNullable())
                typeReference.IsNullable = true;

            ValueReference defaultValue = ResolveDefaultValue(property, typeReference, source, line, column, logger);
            SerializationBehavior serializationBehavior = ResolveSerializationBehavior(property);
            DateTimeKind dateTimeKind = ResolveDateTimeKind(property);
            bool isPartOfKey = ResolveIsPartOfKey(property);
            bool isOptional = ResolveIsOptional(property);
            bool isDiscriminator = ResolveIsDiscriminator(property);
            bool isObfuscated = ResolveIsObfuscated(property);
            bool isRelativeHttpsUrl = ResolveIsRelativeHttpsUrl(property);
            return new ObjectSchemaProperty(name: new Token<string>(property.Name, source, line, column), typeReference, defaultValue, serializationBehavior, dateTimeKind, isPartOfKey, isOptional, isDiscriminator, isObfuscated, isRelativeHttpsUrl);
        }

        private static bool ResolveIsPartOfKey(MemberInfo member) => IsDefined(member, "System.ComponentModel.DataAnnotations.KeyAttribute");
        
        private static bool ResolveIsOptional(MemberInfo member) => IsDefined(member, "Dibix.OptionalAttribute");
        
        private static bool ResolveIsDiscriminator(MemberInfo member) => IsDefined(member, "Dibix.DiscriminatorAttribute");

        private static ValueReference ResolveDefaultValue(MemberInfo member, TypeReference targetType, string source, int line, int column, ILogger logger)
        {
            foreach (CustomAttributeData customAttributeData in member.GetCustomAttributesData())
            {
                if (customAttributeData.AttributeType.FullName != typeof(DefaultValueAttribute).FullName) 
                    continue;

                CustomAttributeTypedArgument argument = customAttributeData.ConstructorArguments.Single();
                ValueReference defaultValue = RawValueReferenceParser.Parse(targetType, argument.Value, source, line, column, logger);
                return defaultValue;
            }

            return null;
        }

        private static SerializationBehavior ResolveSerializationBehavior(MemberInfo member)
        {
            foreach (CustomAttributeData attribute in member.GetCustomAttributesData())
            {
                if (attribute.AttributeType.FullName == typeof(JsonIgnoreAttribute).FullName)
                    return SerializationBehavior.Never;

                if (attribute.AttributeType.FullName != typeof(JsonPropertyAttribute).FullName)
                    continue;

                bool ignoreWhenNull = attribute.NamedArguments
                                               .Where(x => x.MemberName == nameof(JsonPropertyAttribute.NullValueHandling))
                                               .Select(x => (NullValueHandling)x.TypedValue.Value)
                                               .Any(x => x == NullValueHandling.Ignore);
                
                if (ignoreWhenNull)
                    return SerializationBehavior.IfNotEmpty;
            }

            return SerializationBehavior.Always;
        }

        private static DateTimeKind ResolveDateTimeKind(MemberInfo member)
        {
            DateTimeKind dateTimeKind = member.GetCustomAttributesData()
                                              .Where(x => x.AttributeType.FullName == "Dibix.DateTimeKindAttribute")
                                              .Select(x => (DateTimeKind)x.ConstructorArguments.Single().Value)
                                              .SingleOrDefault();
            return dateTimeKind;
        }

        private static bool ResolveIsObfuscated(MemberInfo member) => IsDefined(member, "Dibix.ObfuscatedAttribute");
        
        private static bool ResolveIsRelativeHttpsUrl(MemberInfo member) => IsDefined(member, "Dibix.RelativeHttpsUrlAttribute");

        private static bool IsDefined(MemberInfo member, string attributeName) => member.GetCustomAttributesData().Any(x => x.AttributeType.FullName == attributeName);

        private static Type GetUnderlyingEnumerableType(Type type)
        {
            if (type == typeof(string))
                return null; // string = IEnumerable<char>

            if (type == typeof(byte[]))
                return null; // byte[] => Binary

            return type.GetInterfaces()
                       .Prepend(type)
                       .Select(GetUnderlyingEnumerableTypeCore)
                       .FirstOrDefault(x => x != null);
        }

        private static Type GetUnderlyingEnumerableTypeCore(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GenericTypeArguments.Single();
            
            return null;
        }

        private static SchemaDefinitionSource DetermineSchemaDefinitionSource(Type type)
        {
            bool isRuntimeType = type.Assembly.GetName().Name == "Dibix";
            return isRuntimeType ? SchemaDefinitionSource.Internal : SchemaDefinitionSource.Foreign;
        }
        #endregion
    }
}