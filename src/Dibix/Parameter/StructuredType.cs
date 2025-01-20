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