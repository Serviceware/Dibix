using System;

namespace Dibix.Sdk
{
    internal static class ConstraintTypeExtensions
    {
        public static string ToDisplayName(this ConstraintType type) => $"{String.Join(" ", type.ToString().SplitWords())} constraint";
    }
}