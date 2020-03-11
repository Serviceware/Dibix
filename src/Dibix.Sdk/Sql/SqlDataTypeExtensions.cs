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
          , IErrorReporter errorReporter
          , out string udtTypeName)
        {
            string typeImplementationName = hints.SingleHintValue(SqlHint.ClrType);
            TypeReference typeReference = null;
            if (!String.IsNullOrEmpty(typeImplementationName)) 
                typeReference = typeResolver.ResolveType(typeImplementationName, @namespace, source, dataTypeReference.StartLine, dataTypeReference.StartColumn, false);

            switch (dataTypeReference)
            {
                case SqlDataTypeReference sqlDataTypeReference:
                    udtTypeName = null;

                    if (typeReference != null)
                        return typeReference;

                    if (PrimitiveTypeMap.TryGetValue(sqlDataTypeReference.SqlDataTypeOption, out PrimitiveDataType dataType)) 
                        return new PrimitiveTypeReference(dataType, isNullable, false);

                    errorReporter.RegisterError(source, dataTypeReference.StartLine, dataTypeReference.StartColumn, null, $@"Unsupported sql data type
Name: {name}
DataType: {sqlDataTypeReference.SqlDataTypeOption}");
                    return null;

                case UserDataTypeReference userDataTypeReference:
                    if (String.Equals(userDataTypeReference.Name.BaseIdentifier.Value, "SYSNAME", StringComparison.OrdinalIgnoreCase))
                    {
                        udtTypeName = null;
                        return new PrimitiveTypeReference(PrimitiveDataType.String, isNullable, false);
                    }

                    udtTypeName = $"[{userDataTypeReference.Name.SchemaIdentifier.Value}].[{userDataTypeReference.Name.BaseIdentifier.Value}]";
                    if (typeReference == null)
                    {
                        errorReporter.RegisterError(source, dataTypeReference.StartLine, dataTypeReference.StartColumn, null, $@"Could not determine type implementation for user defined type
Name: {name}
UDT type: {udtTypeName}
Please mark it with /* @ClrType <ClrTypeName> */");
                    }
                    return typeReference;

                case XmlDataTypeReference _:
                    udtTypeName = null;
                    return new PrimitiveTypeReference(PrimitiveDataType.Xml, false, false);

                default:
                    throw new InvalidOperationException($@"Unexpected data type reference
Name: {name}
DataType: {dataTypeReference.GetType()}");
            }
        }
    }
}
