using System.Diagnostics;
using System.Threading;

namespace Dibix.Testing.Data
{
    internal static class ConcurrentTestDibixTraceRedirector
    {
        private static int _initialized;
        private static TextWriterTraceListener _sharedListener;

        public static void Register(DibixTraceSource[] traceSources)
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            _sharedListener = new TextWriterTraceListener(ConcurrentTestTextWriter.Instance);

            foreach (DibixTraceSource traceSource in traceSources)
            {
                traceSource.AddListener(_sharedListener, SourceLevels.Information);
            }
        }

        public static void Unregister(DibixTraceSource[] traceSources)
        {
            if (_sharedListener != null)
            {
                foreach (DibixTraceSource traceSource in traceSources)
                {
                    traceSource.RemoveListener(_sharedListener);
                }
            }

            _initialized = 0;
            _sharedListener = null;
        }
    }
}