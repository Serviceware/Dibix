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
            if (_originalConsoleOut != null)
                Console.SetOut(_originalConsoleOut);

            _initialized = 0;
            _originalConsoleOut = null;
        }
    }
}