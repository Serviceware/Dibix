using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
            content = Regex.Replace(content, @"[^\r](\n)", "\r\n");
            foreach (string line in content.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                writer.WriteLine(line);
            }
        }

        protected static void WriteModifiers(StringWriter writer, CSharpModifiers modifiers, bool indent = true)
        {
            IEnumerable<CSharpModifiers> flags = Enum.GetValues(typeof(CSharpModifiers))
                                                     .Cast<CSharpModifiers>()
                                                     .Where(x => x != default(CSharpModifiers) && modifiers.HasFlag(x));

            if (indent)
                writer.WriteIndent();

            foreach (CSharpModifiers flag in flags)
                writer.WriteRaw(flag.ToString().ToLowerInvariant())
                      .WriteRaw(' ');
        }
    }
}