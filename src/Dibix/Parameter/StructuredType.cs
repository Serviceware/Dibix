using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
#if NET
using Microsoft.Data.SqlClient.Server;
#else
using Microsoft.SqlServer.Server;
#endif

namespace Dibix
{
    public abstract class StructuredType : IEnumerable<SqlDataRecord>
    {
        #region Fields
        private static readonly ConcurrentDictionary<Type, SqlMetaData[]> MetadataCache = new ConcurrentDictionary<Type, SqlMetaData[]>();
        private readonly SqlMetaData[] _metadata;
        private readonly List<SqlDataRecord> _records;
        #endregion

        #region Properties
        public abstract string TypeName { get; }
        #endregion

        #region Constructor
        protected StructuredType()
        {
            _records = new List<SqlDataRecord>();
            _metadata = MetadataCache.GetOrAdd(GetType(), CollectMetadata);
        }
        #endregion

        #region Public Methods
        public IReadOnlyCollection<SqlDataRecord> GetRecords() => _records;

        public SqlMetaData[] GetMetadata() => _metadata;

        public string Dump(bool truncate = false) => SqlDataRecordDiagnostics.Dump(_metadata, _records, truncate);
        #endregion

        #region Protected Methods
        protected internal void AddRecord(params object[] values)
        {
            SqlDataRecord record = new SqlDataRecord(_metadata);
            ValidateParameterLengths(values);
            record.SetValues(values);
            _records.Add(record);
        }

        protected abstract void CollectMetadata(ISqlMetadataCollector collector);
        #endregion

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator() => _records.GetEnumerator();
        #endregion

        #region IEnumerable<SqlDataRecord> Members
        IEnumerator<SqlDataRecord> IEnumerable<SqlDataRecord>.GetEnumerator() => _records.GetEnumerator();
        #endregion

        #region Private Methods
        private SqlMetaData[] CollectMetadata(Type type)
        {
            SqlMetadataCollector collector = new SqlMetadataCollector();
            CollectMetadata(collector);

            if (collector.Metadata.Count == 0)
                throw new InvalidOperationException("Metadata must not be empty");

            SqlMetaData[] metadata = collector.Metadata.ToArray();
            return metadata;
        }

        private void ValidateParameterLengths(object[] row)
        {
            int rowIndex = _records.Count;
            for (int i = 0; i < row.Length; i++)
            {
                object value = row[i];
                SqlMetaData metadata = _metadata[i];
                ValidateParameterLength(value, metadata, rowIndex);
            }
        }
        private static void ValidateParameterLength(object value, SqlMetaData metadata, int rowIndex)
        {
            switch (value)
            {
                case string stringValue:
                    ValidateParameterLength(value, metadata, rowIndex, stringValue.Length);
                    break;

                case byte[] binaryValue:
                    ValidateParameterLength(value, metadata, rowIndex, binaryValue.Length);
                    break;
            }
        }
        private static void ValidateParameterLength(object value, SqlMetaData metadata, int rowIndex, int length)
        {
            if (metadata.MaxLength < 0)
                return;

            if (length > metadata.MaxLength)
            {
                throw new InvalidOperationException($"""
                                                  The value at row {rowIndex} for column '{metadata.Name}' has a length of {length} which exceeds the maximum length of the data type ({metadata.MaxLength})
                                                  -
                                                  Value: {value}
                                                  """);
            }
        }
        #endregion

        #region Nested Types
        private sealed class SqlMetadataCollector : ISqlMetadataCollector
        {
            public ICollection<SqlMetaData> Metadata { get; } = new List<SqlMetaData>();

            public void RegisterMetadata(string name, SqlDbType type) => Metadata.Add(new SqlMetaData(name, type));
            public void RegisterMetadata(string name, SqlDbType type, long maxLength) => Metadata.Add(new SqlMetaData(name, type, maxLength));
            public void RegisterMetadata(string name, SqlDbType type, byte precision, byte scale) => Metadata.Add(new SqlMetaData(name, type, precision, scale));
        }
        #endregion
    }

    public abstract class StructuredType<TDefinition> : StructuredType where TDefinition : StructuredType, new()
    {
        public static TDefinition From<TSource>(IEnumerable<TSource> source, Action<TDefinition, TSource> addItemFunc)
        {
            return From(source, (x, y, _) => addItemFunc(x, y));
        }
        public static TDefinition From<TSource>(IEnumerable<TSource> source, Action<TDefinition, TSource, int> addItemFunc)
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsNotNull(addItemFunc, nameof(addItemFunc));

            TDefinition type = new TDefinition();
            int index = 1;
            foreach (TSource item in source)
            {
                addItemFunc(type, item, index++);
            }
            return type;
        }
    }
}