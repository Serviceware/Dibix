using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Dibix.Sdk.CodeGeneration.Ast;

namespace Dibix.Sdk.CodeGeneration
{
    public class CSharpWriter
    {
        private readonly string _namespace;
        private readonly StringWriter _writer;
        private readonly ICollection<string> _usings;
        private readonly IList<CSharpStatement> _statements;

        private CSharpWriter(StringWriter writer, string @namespace)
        {
            this._writer = writer;
            this._namespace = @namespace;
            this._usings = new SortedSet<string>(new UsingComparer());
            this._statements = new Collection<CSharpStatement>();
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

        public CSharpWriter AddSeparator()
        {
            this._statements.Add(new CSharpSeparator());
            return this;
        }

        public CSharpClass AddClass(string name, CSharpModifiers modifiers, string annotation = null)
        {
            CSharpClass @class = new CSharpClass(name, modifiers, annotation);
            this._statements.Add(@class);
            return @class;
        }

        public IDisposable CreateRegion(string regionName)
        {
            this._statements.Add(new CSharpRegionStart(regionName));
            return Disposable.Create(() => this._statements.Add(new CSharpRegionEnd()));
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

            for (int i = 0; i < this._statements.Count; i++)
            {
                CSharpStatement type = this._statements[i];
                type.Write(this._writer);
                //if (i + 1 < this._types.Count)
                    this._writer.WriteLine();
            }

            this._writer
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

        private class Disposable : IDisposable
        {
            private volatile Action _dispose;

            private Disposable(Action dispose)
            {
                Debug.Assert(dispose != null);
                this._dispose = dispose;
            }

            public static IDisposable Create(Action dispose)
            {
                Guard.IsNotNull(dispose, nameof(dispose));
                return new Disposable(dispose);
            }

            public void Dispose()
            {
#pragma warning disable 0420
                Action dispose = Interlocked.Exchange(ref this._dispose, null);
#pragma warning restore 0420
                dispose?.Invoke();
            }
        }
    }
}