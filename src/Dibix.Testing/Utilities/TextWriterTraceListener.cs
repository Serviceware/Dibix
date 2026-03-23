using System.Diagnostics;
using System.IO;

namespace Dibix.Testing
{
    internal sealed class TextWriterTraceListener : TraceListener
    {
        private readonly TextWriter _inner;

        public TextWriterTraceListener(TextWriter inner) => _inner = inner;

        public override void Write(string message) { } // Ignore trace category prefix stuff

        public override void WriteLine(string message) => _inner.WriteLine(message);
    }
}