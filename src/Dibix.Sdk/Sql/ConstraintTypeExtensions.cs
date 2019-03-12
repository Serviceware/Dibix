using System;

namespace Dibix.Sdk.Sql
{
    internal static class ConstraintTypeExtensions
    {
        public static string ToDisplayName(this ConstraintType type) => $"{String.Join(" ", type.ToString().SplitWords())} constraint";
    }
}