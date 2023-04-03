using System;

namespace Dibix.Testing
{
    internal static class DateTimeExtensions
    {
        public static DateTime RemoveMilliseconds(this DateTime dateTime) => dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
    }
}