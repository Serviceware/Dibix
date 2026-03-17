using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Dibix.Testing
{
    internal static class ThreadSafeTraceRedirector
    {
        private static readonly AsyncLocal<TextWriter> ThreadLocalWriter = new AsyncLocal<TextWriter>();
        private static readonly object Lock = new object();
        private static int _refCount;
        private static ThreadSafeTraceListener _sharedListener;

        public static void RedirectForCurrentContext(TextWriter writer)
        {
            lock (Lock)
            {
                if (_refCount == 0)
                {
                    _sharedListener = new ThreadSafeTraceListener();
                    Trace.Listeners.Add(_sharedListener);
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

                if (_refCount != 0 || _sharedListener == null)
                    return;

                Trace.Listeners.Remove(_sharedListener);
                _sharedListener = null;
            }
        }

        private class ThreadSafeTraceListener : TraceListener
        {
            public override void Write(string message)
            {
                TextWriter writer = ThreadLocalWriter.Value;
                writer?.Write(message);
            }

            public override void WriteLine(string message)
            {
                TextWriter writer = ThreadLocalWriter.Value;
                writer?.WriteLine(message);
            }
        }
    }
}