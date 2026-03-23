using System.Diagnostics;
using System.Threading;

namespace Dibix.Testing
{
    internal static class ConcurrentTestTraceRedirector
    {
        private static int _initialized;
        private static TextWriterTraceListener _sharedListener;

        public static void Register()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            _sharedListener = new TextWriterTraceListener(ConcurrentTestTextWriter.Instance);
            Trace.Listeners.Add(_sharedListener);
        }

        public static void Unregister()
        {
            if (_sharedListener != null)
                Trace.Listeners.Remove(_sharedListener);

            _initialized = 0;
            _sharedListener = null;
        }
    }
}