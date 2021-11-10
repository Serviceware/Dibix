using System;

namespace Dibix.Sdk.CodeGeneration
{
    [Flags]
    public enum SchemaDefinitionSource
    {
        Unknown = 0,
        Local = 1,
        Foreign = 2,
        Generated = 4,
        Internal = 8
    }
}