using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpRoot : CSharpStatement
    {
        private readonly CSharpStatementScope _body;
        private readonly ICollection<string> _usings;

        public CSharpStatementScope Output => this._body;

        public CSharpRoot(CSharpStatementScope body, IEnumerable<CSharpAnnotation> annotations) : base(MarkAnnotationsAsGlobal(annotations))
        {
            this._body = body;
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

            if (base.WriteAnnotations(writer))
                writer.WriteLine();

            this.WriteBody(writer);
        }

        protected override void WriteBody(StringWriter writer) => this._body.Write(writer);

        private static IEnumerable<CSharpAnnotation> MarkAnnotationsAsGlobal(IEnumerable<CSharpAnnotation> globalAnnotations)
        {
            IEnumerable<CSharpAnnotation> globalAnnotationsEnumerated = globalAnnotations as ICollection<CSharpAnnotation> ?? globalAnnotations.ToArray();
            globalAnnotationsEnumerated.Each(x => x.IsGlobal = true);
            return globalAnnotationsEnumerated;
        }

        private class UsingComparer : Comparer<string>
        {
            public override int Compare(string x, string y)
            {
                bool xIsAlias = x != null && x.Contains("=");
                bool yIsAlias = y != null && y.Contains("=");

                if (xIsAlias && yIsAlias)
                    return Default.Compare(x, y);

                if (xIsAlias)
                    return 1;

                if (yIsAlias)
                    return -1;

                bool xIsSystem = x != null && x.StartsWith("System", StringComparison.OrdinalIgnoreCase);
                bool yIsSystem = y != null && y.StartsWith("System", StringComparison.OrdinalIgnoreCase);

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