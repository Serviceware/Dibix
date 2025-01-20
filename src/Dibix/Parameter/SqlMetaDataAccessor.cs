using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if NET
using Microsoft.Data.SqlClient.Server;
#else
using Microsoft.SqlServer.Server;
#endif

namespace Dibix
{
    internal static class SqlMetaDataAccessor
    {
        public const int DefaultMaxLength = -1;
        public const int DefaultPrecision = 19;
        public const int DefaultScale = 2;

        private static readonly ConcurrentDictionary<Type, SqlMetaData[]> Cache = new ConcurrentDictionary<Type, SqlMetaData[]>();

        public static SqlMetaData[] GetMetadata(Type structuredType, LambdaExpression metadataMethodPointerExpression)
        {
            if (!Cache.TryGetValue(structuredType, out SqlMetaData[] metadata))
            {
                metadata = ReadMetadata(metadataMethodPointerExpression);
                Cache.TryAdd(structuredType, metadata);
            }
            return metadata;
        }

        private static SqlMetaData[] ReadMetadata(LambdaExpression metadataMethodPointerExpression)
        {
            MethodCallExpression methodExpression = metadataMethodPointerExpression.Body as MethodCallExpression;
            if (methodExpression == null)
                throw new ArgumentException("Not a valid method expression", nameof(metadataMethodPointerExpression));

            return methodExpression.Method.GetParameters().Select(ReadMetadata).ToArray();
        }

        private static SqlMetaData ReadMetadata(ParameterInfo parameter)
        {
            SqlDbType dbType = DbTypeMap.ToSqlDbType(parameter.ParameterType);
            SqlMetadataAttribute attribute = parameter.GetCustomAttribute<SqlMetadataAttribute>();

            if (dbType == SqlDbType.Decimal)
            {
                byte? precision = attribute?.Precision;
                byte? scale = attribute?.Scale;
                return new SqlMetaData(parameter.Name, dbType, precision ?? DefaultPrecision, scale ?? DefaultScale);
            }

            int? maxLength = attribute?.MaxLength;
            if (dbType == SqlDbType.NVarChar || dbType == SqlDbType.VarBinary || maxLength.HasValue)
                return new SqlMetaData(parameter.Name, dbType, maxLength ?? DefaultMaxLength);

            return new SqlMetaData(parameter.Name, dbType);
        }
    }
}