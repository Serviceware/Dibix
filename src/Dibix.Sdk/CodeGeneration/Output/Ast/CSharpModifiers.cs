using System;

namespace Dibix.Sdk.CodeGeneration
{
    [Flags]
    public enum CSharpModifiers
    {
        None = 0,
        Public = 1,
        Internal = 2,
        Private = 4,
        Static = 8,
        Sealed = 16,
        Const = 32,
        ReadOnly = 64
    }
}