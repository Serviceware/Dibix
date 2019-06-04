using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CSharpRoot : CSharpStatementScope
    {
        private readonly ICollection<string> _usings;

        public CSharpRoot(string @namespace) : base(@namespace)
        {
            this._usings = new SortedSet<string>(new UsingComparer());
        }

        public CSharpRoot AddUsing(string @using)
        {
            this._usings.Add(@using);
            return this;
        }

        public override void Write(StringWriter writer)
        {
            foreach (string @using in this._usings)
                writer.WriteLine($"using {@using};");

            if (this._usings.Any())
                writer.WriteLine();

            base.Write(writer);
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