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
        public static TypeReference ResolveType(Type type, string source, int line, int column, bool isNullable, bool isEnumerable, ISchemaRegistry schemaRegistry)
        {
            if (PrimitiveTypeMap.TryGetValue(type, out PrimitiveDataType dataType))
                return new PrimitiveTypeReference(dataType, isNullable, isEnumerable);

            SchemaTypeReference schemaTypeReference = SchemaTypeReference.WithNamespace(type.Namespace, type.Name, source, line, column, isNullable, isEnumerable);
            if (schemaRegistry.IsRegistered(schemaTypeReference.Key))
                return schemaTypeReference;

            SchemaDefinition schemaDefinition;
            if (type.IsEnum)
            {
                EnumSchema enumSchema = new EnumSchema(type.Namespace, type.Name, false);
                schemaDefinition = enumSchema;
            }
            else
            {
                ObjectSchema objectSchema = new ObjectSchema(type.Namespace, type.Name);
                objectSchema.Properties.AddRange(type.GetProperties()
                                                     .Select(x => new ObjectSchemaProperty(x.Name)));
                schemaDefinition = objectSchema;
            }

            schemaRegistry.Populate(schemaDefinition);

            return schemaTypeReference;
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
                    return this.ResolveType(type, source, line, column, typeName.IsNullable, isEnumerable);
                }

                this._logger.LogError(null, $"Could not locate assembly: {assemblyName}", source, line, column);
                return null;
            }
            catch (Exception ex)
            {
                this._logger.LogError(null, ex.Message, source, line, column);
                return null;
            }
        }

        private TypeReference ResolveType(Type type, string source, int line, int column, bool isNullable, bool isEnumerable) => ResolveType(type, source, line, column, isNullable, isEnumerable, this._schemaRegistry);
        #endregion
    }
}