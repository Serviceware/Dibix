using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Generators
{
    internal readonly struct Annotation
    {
        public string Name { get; }
        public string? Arguments { get; }

        public static Annotation GeneratedCode { get; } = new Annotation("global::System.CodeDom.Compiler.GeneratedCode", $"(\"{ThisAssembly.AssemblyTitle}\", \"{ThisAssembly.AssemblyFileVersion}\")");
        public static Annotation DebuggerNonUserCode { get; } = new Annotation("global::System.Diagnostics.DebuggerNonUserCode");
        public static Annotation ExcludeFromCodeCoverage { get; } = new Annotation("global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage");
        public static Annotation Embedded { get; } = new Annotation("global::Microsoft.CodeAnalysis.Embedded");
        public static IEnumerable<Annotation> Class
        {
            get
            {
                yield return GeneratedCode;
                yield return DebuggerNonUserCode;
                yield return ExcludeFromCodeCoverage;
            }
        }
        public static string ClassText { get; } = String.Join(Environment.NewLine, Class.Select(x => $"    {x}"));

        private Annotation(string name, string? arguments = null)
        {
            Name = name;
            Arguments = arguments;
        }

        public override string ToString() => $"[{Name}{Arguments}]";
    }
}