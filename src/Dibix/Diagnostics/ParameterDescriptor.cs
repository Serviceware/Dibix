using System.Data;

namespace Dibix
{
    internal readonly struct ParameterDescriptor
    {
        public string Name { get; }
        public DbType Type { get; }
        public object Value { get; }
        public int? Size { get; }

        public ParameterDescriptor(string name, DbType type, object value, int? size)
        {
            Name = name;
            Type = type;
            Value = value;
            Size = size;
        }
    }
}