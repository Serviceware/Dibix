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
            TextWriterTraceListener sharedListener = Interlocked.Exchange(ref _sharedListener, null);
            if (sharedListener != null)
                Trace.Listeners.Remove(sharedListener);

            Interlocked.Exchange(ref _initialized, 0);
        }
    }
}