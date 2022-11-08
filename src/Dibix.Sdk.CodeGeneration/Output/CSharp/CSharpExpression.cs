using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public abstract class CSharpExpression
    {
        public abstract void Write(StringWriter writer);

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