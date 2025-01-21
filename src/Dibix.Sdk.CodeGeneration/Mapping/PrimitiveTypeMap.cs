using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class PrimitiveTypeMap
    {
        private static readonly IDictionary<Type, PrimitiveType> ClrTypeMap = new Dictionary<Type, PrimitiveType>
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
          , [typeof(Uri)]            = PrimitiveType.Uri
          , [typeof(Guid)]           = PrimitiveType.UUID
        };
        // System.ReflectionOnlyType <> System.RuntimeType
        private static readonly IDictionary<Guid, PrimitiveType> GuidMap = ClrTypeMap.ToDictionary(x => x.Key.GUID, x => x.Value);
        private static readonly IDictionary<SqlDataTypeOption, PrimitiveType> ScriptDomTypeMap = new Dictionary<SqlDataTypeOption, PrimitiveType>
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
        private static readonly IDictionary<PrimitiveType, SqlDbType> SqlClientTypeMap = new Dictionary<PrimitiveType, SqlDbType>
        {
            [PrimitiveType.Boolean]        = SqlDbType.Bit
          , [PrimitiveType.Byte]           = SqlDbType.TinyInt
          , [PrimitiveType.Int16]          = SqlDbType.SmallInt
          , [PrimitiveType.Int32]          = SqlDbType.Int
          , [PrimitiveType.Int64]          = SqlDbType.BigInt
          , [PrimitiveType.Float]          = SqlDbType.Real
          , [PrimitiveType.Double]         = SqlDbType.Float
          , [PrimitiveType.Decimal]        = SqlDbType.Decimal
          , [PrimitiveType.Binary]         = SqlDbType.VarBinary
          , [PrimitiveType.Stream]         = SqlDbType.VarBinary
          , [PrimitiveType.DateTime]       = SqlDbType.DateTime
          , [PrimitiveType.DateTimeOffset] = SqlDbType.DateTimeOffset
          , [PrimitiveType.String]         = SqlDbType.NVarChar
          , [PrimitiveType.Uri]            = SqlDbType.NVarChar
          , [PrimitiveType.UUID]           = SqlDbType.UniqueIdentifier
          , [PrimitiveType.Xml]            = SqlDbType.Xml
        };
        private static readonly IDictionary<PrimitiveType, Func<OpenApiSchema>> OpenApiTypeMap = new Dictionary<PrimitiveType, Func<OpenApiSchema>>
        {
            [PrimitiveType.Boolean]        = () => new OpenApiSchema { Type = "boolean"                       }
          , [PrimitiveType.Byte]           = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [PrimitiveType.Int16]          = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [PrimitiveType.Int32]          = () => new OpenApiSchema { Type = "integer", Format = "int32"     }
          , [PrimitiveType.Int64]          = () => new OpenApiSchema { Type = "integer", Format = "int64"     }
          , [PrimitiveType.Float]          = () => new OpenApiSchema { Type = "number",  Format = "float"     }
          , [PrimitiveType.Double]         = () => new OpenApiSchema { Type = "number",  Format = "double"    }
          , [PrimitiveType.Decimal]        = () => new OpenApiSchema { Type = "number",  Format = "double"    }
          , [PrimitiveType.Binary]         = () => new OpenApiSchema { Type = "string",  Format = "byte"      }
          , [PrimitiveType.Stream]         = () => new OpenApiSchema { Type = "string",  Format = "binary"    }
          , [PrimitiveType.DateTime]       = () => new OpenApiSchema { Type = "string",  Format = "date-time" }
          , [PrimitiveType.DateTimeOffset] = () => new OpenApiSchema { Type = "string",  Format = "date-time" }
          , [PrimitiveType.String]         = () => new OpenApiSchema { Type = "string"                        }
          , [PrimitiveType.Uri]            = () => new OpenApiSchema { Type = "string",  Format = "uri"       }
          , [PrimitiveType.UUID]           = () => new OpenApiSchema { Type = "string",  Format = "uuid"      }
          , [PrimitiveType.Xml]            = () => new OpenApiSchema { Type = "string"                        }
        };

        public static bool TryGetPrimitiveType(Type clrType, out PrimitiveType primitiveType) => GuidMap.TryGetValue(clrType.GUID, out primitiveType);
        public static bool TryGetPrimitiveType(SqlDataTypeOption sqlDataType, out PrimitiveType primitiveType) => ScriptDomTypeMap.TryGetValue(sqlDataType, out primitiveType);
        
        public static SqlDbType GetSqlDbType(PrimitiveType primitiveType) => SqlClientTypeMap[primitiveType];

        public static Func<OpenApiSchema> GetOpenApiFactory(PrimitiveType primitiveType) => OpenApiTypeMap[primitiveType];
        public static bool TryGetOpenApiFactory(PrimitiveType primitiveType, out Func<OpenApiSchema> factory) => OpenApiTypeMap.TryGetValue(primitiveType, out factory);
    }
}