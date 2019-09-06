using System;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    [Flags]
    public enum CSharpModifiers
    {
        None = 0,
        Public = 1,
        Internal = 2,
        Protected = 4,
        Private = 8,
        Static = 16,
        Sealed = 32,
        Const = 64,
        ReadOnly = 128,
        Override = 256
    }
}