using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Configurations;

namespace Dibix.Testing.TestContainers
{
    public class RedirectStdoutAndStderrToTextWriter : IOutputConsumer
#if !NETFRAMEWORK
        , IAsyncDisposable
#endif
    {
        public bool Enabled => true;
        public Stream Stdout { get; }
        public Stream Stderr { get; }

        public RedirectStdoutAndStderrToTextWriter(TextWriter stdout, TextWriter stderr)
        {
            Stdout = new RedirectToTextWriterStream(stdout);
            Stderr = new RedirectToTextWriterStream(stderr);
        }

        public void Dispose()
        {
            Stdout.Dispose();
            Stderr.Dispose();
        }

#if !NETFRAMEWORK
        public async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            await Stdout.DisposeAsync().ConfigureAwait(false);
            await Stderr.DisposeAsync().ConfigureAwait(false);
        }
#endif

        private sealed class RedirectToTextWriterStream : Stream
        {
            private static readonly Regex AnsiEscapeRegex = new Regex("""\x1B\[[0-9;]*[a-zA-Z]""", RegexOptions.Compiled);
            private readonly TextWriter _writer;

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => default;
            public override long Position { get; set; }

            public RedirectToTextWriterStream(TextWriter writer) => _writer = writer;

            public override void Flush() => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
            {
                string text = Encoding.UTF8.GetString(buffer, offset, count);

                // Strip color codes
                string normalizedText = AnsiEscapeRegex.Replace(text, "");

                _writer.Write(normalizedText);
            }
        }
    }
}