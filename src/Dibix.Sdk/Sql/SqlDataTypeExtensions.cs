using System;
using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    internal static class SqlDataTypeExtensions
    {
        private static readonly IDictionary<SqlDataTypeOption, PrimitiveType> PrimitiveTypeMap = new Dictionary<SqlDataTypeOption, PrimitiveType>
        {
            [SqlDataTypeOption.Bit]              = PrimitiveType.Boolean
          , [SqlDataTypeOption.TinyInt]          = PrimitiveType.Byte
          , [SqlDataTypeOption.SmallInt]         = PrimitiveType.Int16
          , [SqlDataTypeOption.Int]              = PrimitiveType.Int32
          , [SqlDataTypeOption.BigInt]           = PrimitiveType.Int64
          , [SqlDataTypeOption.Real]             = PrimitiveType.Float
          , [SqlDataTypeOption.Float]            = PrimitiveType.Double
          , [SqlDataTypeOption.Decimal]          = PrimitiveType.Decimal
          , [SqlDataTypeOption.SmallMoney]       = PrimitiveType.Decimal
          , [SqlDataTypeOption.Money]            = PrimitiveType.Decimal
          , [SqlDataTypeOption.Numeric]          = PrimitiveType.Decimal
          , [SqlDataTypeOption.Binary]           = PrimitiveType.Binary
          , [SqlDataTypeOption.VarBinary]        = PrimitiveType.Binary
          , [SqlDataTypeOption.Date]             = PrimitiveType.DateTime
          , [SqlDataTypeOption.DateTime]         = PrimitiveType.DateTime
          , [SqlDataTypeOption.DateTime2]        = PrimitiveType.DateTime
          , [SqlDataTypeOption.DateTimeOffset]   = PrimitiveType.DateTimeOffset
          , [SqlDataTypeOption.SmallDateTime]    = PrimitiveType.DateTime
          , [SqlDataTypeOption.Char]             = PrimitiveType.String
          , [SqlDataTypeOption.NChar]            = PrimitiveType.String
          , [SqlDataTypeOption.VarChar]          = PrimitiveType.String
          , [SqlDataTypeOption.NVarChar]         = PrimitiveType.String
          , [SqlDataTypeOption.Text]             = PrimitiveType.String
          , [SqlDataTypeOption.NText]            = PrimitiveType.String
          , [SqlDataTypeOption.UniqueIdentifier] = PrimitiveType.UUID
        };

        public static TypeReference ToTypeReference
        (
            this DataTypeReference dataTypeReference
          , bool isNullable
          , string name
          , string relativeNamespace
          , string source
          , ISqlMarkupDeclaration markup
          , ITypeResolverFacade typeResolver
          , ILogger logger
          , out string udtName
        )
        {
            if (markup.TryGetSingleElementValue(SqlMarkupKey.ClrType, source, logger, out ISqlElementValue typeImplementationName) && !(dataTypeReference is SqlDataTypeReference))
            {
                udtName = null;
                logger.LogError(null, $@"The @ClrType hint is only supported for primitive types
Name: {name}
DataType: {dataTypeReference.GetType()}", source, dataTypeReference.StartLine, dataTypeReference.StartColumn);
                return null;
            }

            switch (dataTypeReference)
            {
                case SqlDataTypeReference sqlDataTypeReference:
                {
                    udtName = null;

                    // Explicit override of generated type name
                    // Examples:
                    // /* @ClrType stream */ [data] VARBINARY(MAX) -> System.IO.Stream data
                    // /* @ClrType RequestTypeEnum */ [requesttype] TINYINT -> RequestTypeEnum requesttype
                    if (typeImplementationName != null)
                    {
                        TypeReference typeReference = typeResolver.ResolveType(typeImplementationName.Value, relativeNamespace, source, typeImplementationName.Line, typeImplementationName.Column, false);
                        if (typeReference != null)
                            typeReference.IsNullable = isNullable;

                        return typeReference;
                    }

                    if (PrimitiveTypeMap.TryGetValue(sqlDataTypeReference.SqlDataTypeOption, out PrimitiveType dataType))
                        return new PrimitiveTypeReference(dataType, isNullable, false, source, dataTypeReference.StartLine, dataTypeReference.StartColumn);

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
                        return new PrimitiveTypeReference(PrimitiveType.String, isNullable, false, source, dataTypeReference.StartLine, dataTypeReference.StartColumn);
                    }

                    udtName = userDataTypeReference.Name.ToFullName();
                    TypeReference typeReference = typeResolver.ResolveType(TypeResolutionScope.UserDefinedType, udtName, relativeNamespace, source, dataTypeReference.StartLine, dataTypeReference.StartColumn, false);
                    return typeReference;
                }

                case XmlDataTypeReference _:
                    udtName = null;
                    return new PrimitiveTypeReference(PrimitiveType.Xml, false, false, source, dataTypeReference.StartLine, dataTypeReference.StartColumn);

                default:
                    throw new InvalidOperationException($@"Unexpected data type reference
Name: {name}
DataType: {dataTypeReference.GetType()}");
            }
        }
    }
}
