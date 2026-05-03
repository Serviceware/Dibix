using System;

namespace Dibix.Sdk.Cli.Tools
{
    internal static class ConsoleUtility
    {
        public static void WriteLineInformation(string message) => Console.WriteLine(message);
        public static void WriteLineDebug(string message) => WriteLine(message, ConsoleColor.DarkGray);
        public static void WriteLineWarning(string message) => WriteLine(message, ConsoleColor.Yellow);
        public static void WriteLineError(string message) => WriteLine(message, ConsoleColor.Red);
        public static void WriteLineSuccess(string message) => WriteLine(message, ConsoleColor.Green);

        private static void WriteLine(string message, ConsoleColor foregroundColor)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = foregroundColor;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}