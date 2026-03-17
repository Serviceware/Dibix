using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Dibix.Testing.Data
{
    internal static class ThreadSafeDibixTraceRedirector
    {
        private static readonly AsyncLocal<TextWriter> ThreadLocalWriter = new AsyncLocal<TextWriter>();
        private static readonly object Lock = new object();
        private static int _refCount;
        private static ThreadSafeDibixTraceListener _sharedListener;

        public static void RedirectForCurrentContext(TextWriter writer, DibixTraceSource[] traceSources)
        {
            lock (Lock)
            {
                if (_refCount == 0)
                {
                    _sharedListener = new ThreadSafeDibixTraceListener();

                    foreach (DibixTraceSource traceSource in traceSources)
                    {
                        traceSource.AddListener(_sharedListener, SourceLevels.Information);
                    }
                }

                _refCount++;
            }

            ThreadLocalWriter.Value = writer;
        }

        public static void RestoreForCurrentContext(DibixTraceSource[] traceSources)
        {
            ThreadLocalWriter.Value = null;

            lock (Lock)
            {
                _refCount--;

                if (_refCount != 0 || _sharedListener == null)
                    return;

                foreach (DibixTraceSource traceSource in traceSources)
                {
                    traceSource.RemoveListener(_sharedListener);
                }

                _sharedListener = null;
            }
        }

        private class ThreadSafeDibixTraceListener : TraceListener
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