using System;
using System.IO;

namespace Dibix
{
    public sealed class FileEntity : IDisposable
    {
        public string Type { get; set; }
        public Stream Data { get; set; }
        public string FileName { get; set; }
        public int? Length { get; set; }

        void IDisposable.Dispose()
        {
            Data?.Dispose();
        }
    }
}