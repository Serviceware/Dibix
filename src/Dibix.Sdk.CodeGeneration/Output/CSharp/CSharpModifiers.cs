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
        Abstract = 32,
        Sealed = 64,
        Const = 128,
        ReadOnly = 256,
        Override = 512,
        Async = 1024
    }
}