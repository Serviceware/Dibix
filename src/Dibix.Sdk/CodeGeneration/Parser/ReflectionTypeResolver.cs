using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ReflectionTypeResolver : TypeResolver
    {
        #region Fields
        private static readonly IDictionary<Type, PrimitiveDataType> PrimitiveTypeMap = new Dictionary<Type, PrimitiveDataType>
        {
            [typeof(bool)]           = PrimitiveDataType.Boolean
          , [typeof(byte)]           = PrimitiveDataType.Byte
          , [typeof(short)]          = PrimitiveDataType.Int16
          , [typeof(int)]            = PrimitiveDataType.Int32
          , [typeof(long)]           = PrimitiveDataType.Int64
          , [typeof(float)]          = PrimitiveDataType.Float
          , [typeof(double)]         = PrimitiveDataType.Double
          , [typeof(decimal)]        = PrimitiveDataType.Decimal
          , [typeof(byte[])]         = PrimitiveDataType.Binary
          , [typeof(DateTime)]       = PrimitiveDataType.DateTime
          , [typeof(DateTimeOffset)] = PrimitiveDataType.DateTimeOffset
          , [typeof(string)]         = PrimitiveDataType.String
          , [typeof(Guid)]           = PrimitiveDataType.UUID
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
        public static TypeReference ResolveType(Type type, string source, int line, int column, string udtName, ISchemaRegistry schemaRegistry) => ResolveType(type, source, line, column, false, false, udtName, schemaRegistry);
        public static TypeReference ResolveType(Type type, string source, int line, int column, ISchemaRegistry schemaRegistry)
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
            return ResolveType(normalizedType, source, line, column, isNullable, isEnumerable, udtName, schemaRegistry);
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

        private TypeReference ResolveType(Type type, string source, int line, int column, bool isNullable, bool isEnumerable) => ResolveType(type, source, line, column, isNullable, isEnumerable, null, this._schemaRegistry);
        private static TypeReference ResolveType(Type type, string source, int line, int column, bool isNullable, bool isEnumerable, string udtName, ISchemaRegistry schemaRegistry)
        {
            if (PrimitiveTypeMap.TryGetValue(type, out PrimitiveDataType dataType))
                return new PrimitiveTypeReference(dataType, isNullable, isEnumerable);

            SchemaTypeReference schemaTypeReference = SchemaTypeReference.WithNamespace(type.Namespace, type.Name, source, line, column, isNullable, isEnumerable);
            if (schemaRegistry.IsRegistered(schemaTypeReference.Key))
                return schemaTypeReference;

            if (!String.IsNullOrEmpty(udtName))
            {
                UserDefinedTypeSchema udtSchema = new UserDefinedTypeSchema(type.Namespace, type.Name, udtName);
                udtSchema.Properties.AddRange(type.GetProperties()
                                                  .Select(x => new ObjectSchemaProperty(x.Name, ResolveType(x.PropertyType, source, line, column, schemaRegistry))));
                schemaRegistry.Populate(udtSchema);
            }
            else if (type.IsEnum)
            {
                EnumSchema enumSchema = new EnumSchema(type.Namespace, type.Name, false);

                // Enum.GetValues() => "The requested operation is invalid in the ReflectionOnly context"
                foreach (FieldInfo member in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    enumSchema.Members.Add(new EnumSchemaMember(member.Name, (int)member.GetRawConstantValue(), null));

                schemaRegistry.Populate(enumSchema);
            }
            else
            {
                ObjectSchema objectSchema = new ObjectSchema(type.Namespace, type.Name);
                schemaRegistry.Populate(objectSchema); // Register schema before traversing properties to avoid endless recursions for self referencing properties
                objectSchema.Properties.AddRange(type.GetProperties()
                                                     .Select(x => CreateProperty(x, source, line, column, schemaRegistry)));
            }

            return schemaTypeReference;
        }

        private static ObjectSchemaProperty CreateProperty(PropertyInfo property, string source, int line, int column, ISchemaRegistry schemaRegistry)
        {
            TypeReference typeReference = ResolveType(property.PropertyType, source, line, column, schemaRegistry);
            if (IsNullableReferenceType(property))
                typeReference.IsNullable = true;

            return new ObjectSchemaProperty(property.Name, typeReference);
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

        private static Type GetUnderlyingEnumerableType(Type type)
        {
            if (type == typeof(string))
                return null; // string = IEnumerable<char>

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