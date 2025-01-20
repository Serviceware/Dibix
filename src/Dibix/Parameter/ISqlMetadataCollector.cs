using System.Data;

namespace Dibix
{
    public interface ISqlMetadataCollector
    {
        void RegisterMetadata(string name, SqlDbType type);
        void RegisterMetadata(string name, SqlDbType type, long maxLength);
        void RegisterMetadata(string name, SqlDbType type, byte precision, byte scale);
    }
}