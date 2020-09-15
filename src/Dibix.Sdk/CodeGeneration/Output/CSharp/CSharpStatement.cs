using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public abstract class CSharpStatement
    {
        private readonly IEnumerable<string> _annotations;
        
        protected CSharpStatement() : this(Enumerable.Empty<string>()) { }
        protected CSharpStatement(IEnumerable<string> annotations)
        {
            this._annotations = annotations;
        }

        public virtual void Write(StringWriter writer)
        {
            foreach (string annotation in this._annotations.OrderBy(x => x.Length))
            {
                this.WriteAnnotation(writer, annotation);
            }
        }

        protected virtual void WriteAnnotation(StringWriter writer, string annotation)
        {
            writer.WriteLine($"[{annotation}]");
        }

        protected static void WriteMultiline(StringWriter writer, string content)
        {
            foreach (string line in content.Split('\n').Select(x => x.TrimEnd('\r')))
            {
                if (!String.IsNullOrEmpty(line)) // Don't indent empty lines
                    writer.WriteLine(line);
                else
                    writer.WriteLine();
            }
        }

        protected static void WriteModifiers(StringWriter writer, CSharpModifiers modifiers, bool indent = true)
        {
            IEnumerable<CSharpModifiers> flags = Enum.GetValues(typeof(CSharpModifiers))
                                                     .Cast<CSharpModifiers>()
                                                     .Where(x => x != default && modifiers.HasFlag(x));

            if (indent)
                writer.WriteIndent();

            foreach (CSharpModifiers flag in flags)
                writer.WriteRaw(flag.ToString().ToLowerInvariant())
                      .WriteRaw(' ');
        }
    }
}