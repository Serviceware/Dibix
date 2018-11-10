using System;

namespace Dibix.Sdk.CodeGeneration
{
    [Flags]
    internal enum CSharpModifiers
    {
        None = 0,
        Public = 1,
        Internal = 2,
        Private = 4,
        Static = 8,
        Sealed = 16
    }
}