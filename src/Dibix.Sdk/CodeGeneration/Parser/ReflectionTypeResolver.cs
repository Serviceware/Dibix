using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ReflectionTypeResolver : TypeResolver
    {
        #region Fields
        private static readonly IDictionary<Type, PrimitiveType> PrimitiveTypeMap = new Dictionary<Type, PrimitiveType>
        {
            [typeof(bool)]           = PrimitiveType.Boolean
          , [typeof(byte)]           = PrimitiveType.Byte
          , [typeof(short)]          = PrimitiveType.Int16
          , [typeof(int)]            = PrimitiveType.Int32
          , [typeof(long)]           = PrimitiveType.Int64
          , [typeof(float)]          = PrimitiveType.Float
          , [typeof(double)]         = PrimitiveType.Double
          , [typeof(decimal)]        = PrimitiveType.Decimal
          , [typeof(byte[])]         = PrimitiveType.Binary
          , [typeof(DateTime)]       = PrimitiveType.DateTime
          , [typeof(DateTimeOffset)] = PrimitiveType.DateTimeOffset
          , [typeof(string)]         = PrimitiveType.String
          , [typeof(Guid)]           = PrimitiveType.UUID
        };
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
        public override TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable)
        {
            bool isAssemblyQualified = input.IndexOf(',') >= 0;
            return !isAssemblyQualified ? this.TryLocalType(input, source, line, column, isEnumerable) : this.TryForeignType(input, source, line, column, isEnumerable);
        }
        #endregion

        #region Public Methods
        public static TypeReference ResolveType(Type type, string source, int line, int column, string udtName, ISchemaRegistry schemaRegistry, ILogger logger) => ResolveType(type, source, line, column, false, false, udtName, schemaRegistry, logger);
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
                    return ReflectionOnlyTypeInspector.Inspect(() => this.ResolveType(type, source, line, column, typeName.IsNullable, isEnumerable));
                }

                this._logger.LogError(null, $"Could not locate assembly: {assemblyName}", source, line, column + parts[0].Length + 1);
                return null;
            }
            catch (Exception ex)
            {
                this._logger.LogError(null, ex.Message, source, line, column);
                return null;
            }
        }

        private TypeReference ResolveType(Type type, string source, int line, int column, bool isNullable, bool isEnumerable) => ResolveType(type, source, line, column, isNullable, isEnumerable, null, this._schemaRegistry, this._logger);
        private static TypeReference ResolveType(Type type, string source, int line, int column, bool isNullable, bool isEnumerable, string udtName, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (PrimitiveTypeMap.TryGetValue(type, out PrimitiveType dataType))
                return new PrimitiveTypeReference(dataType, isNullable, isEnumerable);

            SchemaTypeReference schemaTypeReference = SchemaTypeReference.WithNamespace(type.Namespace, type.Name, source, line, column, isNullable, isEnumerable);
            if (schemaRegistry.IsRegistered(schemaTypeReference.Key))
                return schemaTypeReference;

            if (!String.IsNullOrEmpty(udtName))
            {
                UserDefinedTypeSchema udtSchema = new UserDefinedTypeSchema(type.Namespace, type.Name, udtName);
                MethodInfo addMethod = type.GetMethod("Add");
                if (addMethod == null)
                    throw new InvalidOperationException($"Could not find 'Add' method on type: {type}");

                udtSchema.Properties.AddRange(addMethod.GetParameters()
                                                       .Select(x => new ObjectSchemaProperty(x.Name, ResolveType(x.ParameterType, source, line, column, schemaRegistry, logger))));
                schemaRegistry.Populate(udtSchema);
            }
            else if (type.IsEnum)
            {
                EnumSchema enumSchema = new EnumSchema(type.Namespace, type.Name, false);

                // Enum.GetValues() => "The requested operation is invalid in the ReflectionOnly context"
                foreach (FieldInfo member in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    enumSchema.Members.Add(new EnumSchemaMember(member.Name, (int)member.GetRawConstantValue(), stringValue: null, enumSchema));

                schemaRegistry.Populate(enumSchema);
            }
            else
            {
                ObjectSchema objectSchema = new ObjectSchema(type.Namespace, type.Name);
                schemaRegistry.Populate(objectSchema); // Register schema before traversing properties to avoid endless recursions for self referencing properties
                objectSchema.Properties.AddRange(type.GetProperties()
                                                     .Select(x => CreateProperty(x, source, line, column, schemaRegistry, logger)));
            }

            return schemaTypeReference;
        }

        private static ObjectSchemaProperty CreateProperty(PropertyInfo property, string source, int line, int column, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            TypeReference typeReference = ResolveType(property.PropertyType, source, line, column, schemaRegistry, logger);
            if (IsNullableReferenceType(property))
                typeReference.IsNullable = true;

            bool isPartOfKey = ResolveIsPartOfKey(property);
            bool isOptional = ResolveIsOptional(property);
            bool isDiscriminator = ResolveIsDiscriminator(property);
            DefaultValue defaultValue = ResolveDefaultValue(property, source, line, column, schemaRegistry, logger);
            SerializationBehavior serializationBehavior = ResolveSerializationBehavior(property);
            DateTimeKind dateTimeKind = ResolveDateTimeKind(property);
            bool obfuscated = ResolveObfuscated(property);
            return new ObjectSchemaProperty(property.Name, typeReference, isPartOfKey, isOptional, isDiscriminator, defaultValue, serializationBehavior, dateTimeKind, obfuscated);
        }

        private static bool IsNullableReferenceType(PropertyInfo property)
        {
            if (property.PropertyType.IsValueType)
                return false;

            byte nullableFlag = ResolveNullableFlag(property);
            return nullableFlag == 2;
        }

        private static byte ResolveNullableFlag(MemberInfo member)
        {
            byte nullableFlag = ResolveNullableFlag(member,               "System.Runtime.CompilerServices.NullableAttribute")
                             ?? ResolveNullableFlag(member.DeclaringType, "System.Runtime.CompilerServices.NullableContextAttribute") 
                             ?? 0;
            return nullableFlag;
        }

        private static byte? ResolveNullableFlag(MemberInfo member, string attributeFullName)
        {
            foreach (CustomAttributeData attribute in member.GetCustomAttributesData())
            {
                if (attribute.AttributeType.FullName != attributeFullName) 
                    continue;

                byte nullableFlag = attribute.ConstructorArguments
                                             .Select(y => y.Value)
                                             .OfType<byte>()
                                             .SingleOrDefault();
                return nullableFlag;
            }

            return null;
        }

        private static bool ResolveIsPartOfKey(MemberInfo member) => IsDefined(member, "System.ComponentModel.DataAnnotations.KeyAttribute");
        
        private static bool ResolveIsOptional(MemberInfo member) => IsDefined(member, "Dibix.OptionalAttribute");
        
        private static bool ResolveIsDiscriminator(MemberInfo member) => IsDefined(member, "Dibix.DiscriminatorAttribute");

        private static DefaultValue ResolveDefaultValue(MemberInfo member, string source, int line, int column, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            foreach (CustomAttributeData customAttributeData in member.GetCustomAttributesData())
            {
                if (customAttributeData.AttributeType.FullName != typeof(DefaultValueAttribute).FullName) 
                    continue;

                CustomAttributeTypedArgument argument = customAttributeData.ConstructorArguments.Single();
                DefaultValue defaultValue = new DefaultValue(argument.Value, source, line, column);
                defaultValue.EnumMember = CollectEnumDefault(argument.ArgumentType, argument.Value, source, line, column, schemaRegistry, logger);
                return defaultValue;
            }

            return null;
        }

        private static EnumSchemaMember CollectEnumDefault(Type argumentType, object value, string source, int line, int column, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (!(value is int)) 
                return null;

            TypeReference typeReference = ResolveType(argumentType, source, line, column, isNullable: false, isEnumerable: false, udtName: null, schemaRegistry, logger);
            if (!(typeReference is SchemaTypeReference schemaTypeReference)
             || !(schemaRegistry.GetSchema(schemaTypeReference) is EnumSchema enumSchema)) 
                return null;

            EnumSchemaMember enumMember = enumSchema.Members.FirstOrDefault(x => Equals(x.ActualValue, value));
            if (enumMember != null)
                return enumMember;

            logger.LogError(code: null, $"Enum '{enumSchema.FullName}' does not define a member with value '{value}'", source, line, column);
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

        private static bool ResolveObfuscated(MemberInfo member) => IsDefined(member, "Dibix.ObfuscatedAttribute");

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
        #endregion
    }
}