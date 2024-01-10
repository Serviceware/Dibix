using System.Collections.Generic;

namespace Dibix.Generators
{
    internal readonly struct Annotation
    {
        public string Name { get; }
        public string? Arguments { get; }

        public static Annotation GeneratedCode = new Annotation("global::System.CodeDom.Compiler.GeneratedCode", $"(\"{ThisAssembly.AssemblyTitle}\", \"{ThisAssembly.AssemblyFileVersion}\")");
        public static Annotation DebuggerNonUserCode = new Annotation("global::System.Diagnostics.DebuggerNonUserCode");
        public static Annotation ExcludeFromCodeCoverage = new Annotation("global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage");
        public static IEnumerable<Annotation> All
        {
            get
            {
                yield return GeneratedCode;
                yield return DebuggerNonUserCode;
                yield return ExcludeFromCodeCoverage;
            }
        }

        private Annotation(string name, string? arguments = null)
        {
            this.Name = name;
            this.Arguments = arguments;
        }
    }
}