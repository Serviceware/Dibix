using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.OpenApi;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class PrimitiveTypeMap
    {
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
          , [SqlDataTypeOption.Date]             = PrimitiveType.Date
          , [SqlDataTypeOption.Time]             = PrimitiveType.Time
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
          , [PrimitiveType.Date]           = SqlDbType.Date
          , [PrimitiveType.Time]           = SqlDbType.Time
          , [PrimitiveType.DateTime]       = SqlDbType.DateTime
          , [PrimitiveType.DateTimeOffset] = SqlDbType.DateTimeOffset
          , [PrimitiveType.String]         = SqlDbType.NVarChar
          , [PrimitiveType.Uri]            = SqlDbType.NVarChar
          , [PrimitiveType.UUID]           = SqlDbType.UniqueIdentifier
          , [PrimitiveType.Xml]            = SqlDbType.Xml
        };
        private static readonly IDictionary<PrimitiveType, Func<OpenApiSchema>> OpenApiTypeMap = new Dictionary<PrimitiveType, Func<OpenApiSchema>>
        {
            [PrimitiveType.Boolean]        = () => new OpenApiSchema { Type = JsonSchemaType.Boolean          }
          , [PrimitiveType.Byte]           = () => new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32"     }
          , [PrimitiveType.Int16]          = () => new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32"     }
          , [PrimitiveType.Int32]          = () => new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32"     }
          , [PrimitiveType.Int64]          = () => new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64"     }
          , [PrimitiveType.Float]          = () => new OpenApiSchema { Type = JsonSchemaType.Number,  Format = "float"     }
          , [PrimitiveType.Double]         = () => new OpenApiSchema { Type = JsonSchemaType.Number,  Format = "double"    }
          , [PrimitiveType.Decimal]        = () => new OpenApiSchema { Type = JsonSchemaType.Number,  Format = "double"    }
          , [PrimitiveType.Binary]         = () => new OpenApiSchema { Type = JsonSchemaType.String,  Format = "byte"      }
          , [PrimitiveType.Stream]         = () => new OpenApiSchema { Type = JsonSchemaType.String,  Format = "binary"    }
          , [PrimitiveType.Date]           = () => new OpenApiSchema { Type = JsonSchemaType.String,  Format = "date"      }
          , [PrimitiveType.Time]           = () => new OpenApiSchema { Type = JsonSchemaType.String,  Format = "time"      }
          , [PrimitiveType.DateTime]       = () => new OpenApiSchema { Type = JsonSchemaType.String,  Format = "date-time" }
          , [PrimitiveType.DateTimeOffset] = () => new OpenApiSchema { Type = JsonSchemaType.String,  Format = "date-time" }
          , [PrimitiveType.String]         = () => new OpenApiSchema { Type = JsonSchemaType.String                        }
          , [PrimitiveType.Uri]            = () => new OpenApiSchema { Type = JsonSchemaType.String,  Format = "uri"       }
          , [PrimitiveType.UUID]           = () => new OpenApiSchema { Type = JsonSchemaType.String,  Format = "uuid"      }
          , [PrimitiveType.Xml]            = () => new OpenApiSchema { Type = JsonSchemaType.String                        }
        };

        public static bool TryGetPrimitiveType(SqlDataTypeOption sqlDataType, out PrimitiveType primitiveType) => ScriptDomTypeMap.TryGetValue(sqlDataType, out primitiveType);

        public static SqlDbType GetSqlDbType(PrimitiveType primitiveType) => SqlClientTypeMap[primitiveType];

        public static Func<OpenApiSchema> GetOpenApiFactory(PrimitiveType primitiveType) => OpenApiTypeMap[primitiveType];
        public static bool TryGetOpenApiFactory(PrimitiveType primitiveType, out Func<OpenApiSchema> factory) => OpenApiTypeMap.TryGetValue(primitiveType, out factory);
    }
}