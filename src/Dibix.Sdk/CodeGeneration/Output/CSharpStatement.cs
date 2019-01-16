using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class CSharpStatement
    {
        private readonly string _annotation;
        
        public CSharpStatement(string annotation = null)
        {
            this._annotation = annotation;
        }

        public virtual void Write(StringWriter writer)
        {
            if (!String.IsNullOrEmpty(this._annotation))
                writer.WriteLine($"[{this._annotation}]");
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