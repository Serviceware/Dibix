using System;

namespace Dibix
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class SqlMetadataAttribute : Attribute
    {
        public int MaxLength { get; set; } = SqlMetaDataAccessor.DefaultMaxLength;
        public byte Precision { get; set; } = SqlMetaDataAccessor.DefaultPrecision;
        public byte Scale { get; set; } = SqlMetaDataAccessor.DefaultScale;
    }
}