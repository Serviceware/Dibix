using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Dibix.Testing
{
    internal static class ThreadSafeConsoleRedirector
    {
        private static readonly AsyncLocal<TextWriter> ThreadLocalWriter = new AsyncLocal<TextWriter>();
        private static readonly object Lock = new object();
        private static int _refCount;
        private static TextWriter _originalConsoleOut;

        public static void RedirectForCurrentContext(TextWriter writer)
        {
            lock (Lock)
            {
                if (_refCount == 0)
                {
                    _originalConsoleOut = Console.Out;
                    Console.SetOut(new ThreadAwareTextWriter());
                }

                _refCount++;
            }

            ThreadLocalWriter.Value = writer;
        }

        public static void RestoreForCurrentContext()
        {
            ThreadLocalWriter.Value = null;

            lock (Lock)
            {
                _refCount--;

                if (_refCount != 0 || _originalConsoleOut == null)
                    return;

                Console.SetOut(_originalConsoleOut);
                _originalConsoleOut = null;
            }
        }

        private class ThreadAwareTextWriter : TextWriter
        {
            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(string value)
            {
                TextWriter writer = ThreadLocalWriter.Value ?? _originalConsoleOut;
                writer?.Write(value);
            }

            public override void WriteLine(string value)
            {
                TextWriter writer = ThreadLocalWriter.Value ?? _originalConsoleOut;
                writer?.WriteLine(value);
            }
        }
    }
}