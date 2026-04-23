using System;
using System.IO;
using System.Threading;

namespace Dibix.Testing
{
    internal static class ConcurrentTestConsoleRedirector
    {
        private static int _initialized;
        private static TextWriter _originalConsoleOut;

        public static void Register()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            _originalConsoleOut = Console.Out;
            Console.SetOut(ConcurrentTestTextWriter.Instance);
        }

        public static void Unregister()
        {
            TextWriter originalConsoleOut = Interlocked.Exchange(ref _originalConsoleOut, null);
            if (originalConsoleOut != null)
                Console.SetOut(originalConsoleOut);

            Interlocked.Exchange(ref _initialized, 0);
        }
    }
}