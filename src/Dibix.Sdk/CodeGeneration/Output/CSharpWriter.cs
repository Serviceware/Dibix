using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal class CSharpWriter
    {
        private readonly string _namespace;
        private readonly StringWriter _writer;
        private readonly ICollection<string> _usings;
        private readonly IList<CSharpStatement> _types;

        private CSharpWriter(StringWriter writer, string @namespace)
        {
            this._writer = writer;
            this._namespace = @namespace;
            this._usings = new SortedSet<string>(new UsingComparer());
            this._types = new Collection<CSharpStatement>();
        }

        public static CSharpWriter Init(StringWriter writer, string @namespace)
        {
            return new CSharpWriter(writer, @namespace);
        }

        public CSharpWriter AddUsing(string @using)
        {
            this._usings.Add(@using);
            return this;
        }

        public CSharpClass AddClass(string name, CSharpModifiers modifiers, string annotation = null)
        {
            CSharpClass @class = new CSharpClass(name, modifiers, annotation);
            this._types.Add(@class);
            return @class;
        }

        public string Generate()
        {
            foreach (string @using in this._usings)
                this._writer.WriteLine($"using {@using};");

            if (this._usings.Any())
                this._writer.WriteLine();

            this._writer
                .WriteLine(String.Concat("namespace ", this._namespace))
                .WriteLine("{")
                .PushIndent();

            for (int i = 0; i < this._types.Count; i++)
            {
                CSharpStatement type = this._types[i];
                type.Write(this._writer);
                if (i + 1 < this._types.Count)
                    this._writer.WriteLine();
            }

            this._writer
                .WriteLine()
                .PopIndent()
                .Write("}");

            return this._writer.ToString();
        }

        private class UsingComparer : Comparer<string>
        {
            public override int Compare(string x, string y)
            {
                bool xIsSystem = x.StartsWith("System", StringComparison.OrdinalIgnoreCase);
                bool yIsSystem = y.StartsWith("System", StringComparison.OrdinalIgnoreCase);

                if (xIsSystem && yIsSystem)
                    return Default.Compare(x, y);

                if (xIsSystem)
                    return -1;

                if (yIsSystem)
                    return 1;

                return Default.Compare(x, y);
            }
        }
    }
}