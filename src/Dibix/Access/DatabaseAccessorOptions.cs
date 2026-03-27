using System;
using System.Data;

namespace Dibix
{
    public sealed class DatabaseAccessorOptions
    {
        public bool? BufferResult { get; set; }
        public IDbTransaction DefaultTransaction { get; set; }
        public int? DefaultCommandTimeout { get; set; }
        public Action OnDispose { get; set; }
        public bool AddUdtParameterValueDumpToException { get; set; } = true;
    }
}