using System;
using System.Diagnostics;

namespace Dibix
{
    [DebuggerNonUserCode]
    public static class Guard
    {
        public static void IsNotNull<T>(T value, string propertyName, string message = null) where T : class
        {
            if (value != null)
                return;

            if (!String.IsNullOrEmpty(message))
                throw new ArgumentNullException(propertyName, message);

            throw new ArgumentNullException(propertyName);
        }

        public static void IsNotNullOrEmpty(string value, string propertyName, string message = null)
        {
            if (!String.IsNullOrEmpty(value))
                return;

            if (!String.IsNullOrEmpty(message))
                throw new ArgumentNullException(propertyName, message);

            throw new ArgumentNullException(propertyName);
        }
    }
}