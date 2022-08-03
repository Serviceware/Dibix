using System;
using System.Diagnostics;

namespace Dibix
{
    [DebuggerNonUserCode]
    internal static class Guard
    {
#pragma warning disable CS8625
        public static void IsNotNull<T>(T value, string paramName, string message = null) where T : class
#pragma warning restore CS8625
        {
            if (value != null)
                return;

            if (!String.IsNullOrEmpty(message))
                throw new ArgumentNullException(paramName, message);

            throw new ArgumentNullException(paramName);
        }

#pragma warning disable CS8625
        public static void IsNotNullOrEmpty(string value, string paramName, string message = null)
#pragma warning restore CS8625
        {
            if (!String.IsNullOrEmpty(value))
                return;

            if (!String.IsNullOrEmpty(message))
                throw new ArgumentNullException(paramName, message);

            throw new ArgumentNullException(paramName);
        }
    }
}