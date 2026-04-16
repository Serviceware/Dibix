using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpRoot : CSharpStatement
    {
        private readonly CSharpStatementScope _body;
        private readonly SortedDictionary<string, CSharpExpression> _usings;

        public CSharpStatementScope Output => this._body;
        protected override bool MultilineAnnotations => true;

        public CSharpRoot(CSharpStatementScope body, IEnumerable<CSharpGlobalAnnotation> annotations) : base(annotations)
        {
            this._body = body;
            this._usings = new SortedDictionary<string, CSharpExpression>(new UsingComparer());
        }

        public CSharpRoot AddUsing(string @using) => AddUsing(@using, x => new CSharpUsingStatement(x));
        public CSharpRoot AddUsing(string @using, Func<string, CSharpExpression> valueFactory)
        {
            if (!_usings.ContainsKey(@using))
                _usings.Add(@using, valueFactory(@using));

            return this;
        }

        public override void Write(StringWriter writer)
        {
            int i = 0;
            foreach (CSharpExpression @using in _usings.Values)
            {
                @using.Write(writer);
                i++;

                if (i < _usings.Count)
                    writer.WriteLine();
            }

            if (_usings.Count > 0)
                writer.WriteLine();

            if (this._usings.Any())
                writer.WriteLine();

            if (base.WriteAnnotations(writer))
                writer.WriteLine();

            this.WriteBody(writer);
        }

        protected override void WriteBody(StringWriter writer) => this._body.Write(writer);

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