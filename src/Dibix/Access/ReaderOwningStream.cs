using System;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    internal sealed class ReaderOwningStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly DbDataReader _reader;

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanWrite => _innerStream.CanWrite;
        public override bool CanSeek => _innerStream.CanSeek;
        public override long Length => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public ReaderOwningStream(Stream innerStream, DbDataReader reader)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override void Flush() => _innerStream.Flush();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
                _reader.Dispose(); // Also dispose the reader
            }
            base.Dispose(disposing);
        }
    }
}