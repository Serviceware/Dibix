using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal static class MultiMapUtility
    {
        public static void ValidateParameters(IReadOnlyCollection<Type> types, string splitOn)
        {
            if (types.Count < 2)
                throw new InvalidOperationException("Expected at least 2 types for multi map result");

            int splitOnSegmentCount = splitOn.Split(',').Length + 1;
            if (types.Count != splitOnSegmentCount)
            {
                throw new InvalidOperationException($@"Multi map type count does not match the amount of segments in splitOn. Expected segments: {types.Count - 1}
types: {String.Join(",", types.Select(x => x.FullName))} ({types.Count})
splitOn: {splitOn} ({splitOnSegmentCount})");
            }
        }
    }
}