using System;
using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    internal static class SqlDataTypeExtensions
    {
        private static readonly IDictionary<SqlDataTypeOption, PrimitiveDataType> PrimitiveTypeMap = new Dictionary<SqlDataTypeOption, PrimitiveDataType>
        {
            [SqlDataTypeOption.Bit]              = PrimitiveDataType.Boolean
          , [SqlDataTypeOption.TinyInt]          = PrimitiveDataType.Byte
          , [SqlDataTypeOption.SmallInt]         = PrimitiveDataType.Int16
          , [SqlDataTypeOption.Int]              = PrimitiveDataType.Int32
          , [SqlDataTypeOption.BigInt]           = PrimitiveDataType.Int64
          , [SqlDataTypeOption.Real]             = PrimitiveDataType.Float
          , [SqlDataTypeOption.Float]            = PrimitiveDataType.Double
          , [SqlDataTypeOption.Decimal]          = PrimitiveDataType.Decimal
          , [SqlDataTypeOption.SmallMoney]       = PrimitiveDataType.Decimal
          , [SqlDataTypeOption.Money]            = PrimitiveDataType.Decimal
          , [SqlDataTypeOption.Numeric]          = PrimitiveDataType.Decimal
          , [SqlDataTypeOption.Binary]           = PrimitiveDataType.Binary
          , [SqlDataTypeOption.VarBinary]        = PrimitiveDataType.Binary
          , [SqlDataTypeOption.Date]             = PrimitiveDataType.DateTime
          , [SqlDataTypeOption.DateTime]         = PrimitiveDataType.DateTime
          , [SqlDataTypeOption.DateTime2]        = PrimitiveDataType.DateTime
          , [SqlDataTypeOption.DateTimeOffset]   = PrimitiveDataType.DateTimeOffset
          , [SqlDataTypeOption.SmallDateTime]    = PrimitiveDataType.DateTime
          , [SqlDataTypeOption.Char]             = PrimitiveDataType.String
          , [SqlDataTypeOption.NChar]            = PrimitiveDataType.String
          , [SqlDataTypeOption.VarChar]          = PrimitiveDataType.String
          , [SqlDataTypeOption.NVarChar]         = PrimitiveDataType.String
          , [SqlDataTypeOption.Text]             = PrimitiveDataType.String
          , [SqlDataTypeOption.NText]            = PrimitiveDataType.String
          , [SqlDataTypeOption.UniqueIdentifier] = PrimitiveDataType.UUID
        };

        public static TypeReference ToTypeReference
        (
            this DataTypeReference dataTypeReference
          , bool isNullable
          , string name
          , string @namespace
          , string source
          , IEnumerable<SqlHint> hints
          , ITypeResolverFacade typeResolver
          , ILogger logger
          , out string udtName
        )
        {
            string typeImplementationName = hints.SingleHintValue(SqlHint.ClrType);
            if (!String.IsNullOrEmpty(typeImplementationName) && !(dataTypeReference is SqlDataTypeReference))
            {
                udtName = null;
                logger.LogError(null, $@"The @ClrType hint is only supported for primitive types and is used to specify an enum type implementation
Name: {name}
DataType: {dataTypeReference.GetType()}", source, dataTypeReference.StartLine, dataTypeReference.StartColumn);
                return null;
            }

            switch (dataTypeReference)
            {
                case SqlDataTypeReference sqlDataTypeReference:
                {
                    udtName = null;

                    // Most likely a primitive parameter that should be generated using a known enum, therefore specific type implementation hint
                    if (!String.IsNullOrEmpty(typeImplementationName))
                    {
                        TypeReference typeReference = typeResolver.ResolveType(typeImplementationName, @namespace, source, dataTypeReference.StartLine, dataTypeReference.StartColumn, false);
                        return typeReference;
                    }

                    if (PrimitiveTypeMap.TryGetValue(sqlDataTypeReference.SqlDataTypeOption, out PrimitiveDataType dataType))
                        return new PrimitiveTypeReference(dataType, isNullable, false);

                    logger.LogError(null, $@"Unsupported sql data type
Name: {name}
DataType: {sqlDataTypeReference.SqlDataTypeOption}", source, dataTypeReference.StartLine, dataTypeReference.StartColumn);
                    return null;
                }

                case UserDataTypeReference userDataTypeReference:
                {
                    if (String.Equals(userDataTypeReference.Name.BaseIdentifier.Value, "SYSNAME", StringComparison.OrdinalIgnoreCase))
                    {
                        udtName = null;
                        return new PrimitiveTypeReference(PrimitiveDataType.String, isNullable, false);
                    }

                    udtName = userDataTypeReference.Name.ToFullName();
                    TypeReference typeReference = typeResolver.ResolveType(TypeResolutionScope.UserDefinedType, udtName, @namespace, source, dataTypeReference.StartLine, dataTypeReference.StartColumn, false);
                    return typeReference;
                }

                case XmlDataTypeReference _:
                    udtName = null;
                    return new PrimitiveTypeReference(PrimitiveDataType.Xml, false, false);

                default:
                    throw new InvalidOperationException($@"Unexpected data type reference
Name: {name}
DataType: {dataTypeReference.GetType()}");
            }
        }
    }
}
