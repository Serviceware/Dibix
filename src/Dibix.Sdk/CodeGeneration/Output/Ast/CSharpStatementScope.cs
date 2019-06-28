using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Dibix.Sdk.CodeGeneration
{
    public class CSharpStatementScope : CSharpStatement
    {
        private readonly string _namespace;
        private readonly IList<CSharpStatement> _statements;

        protected CSharpStatementScope(string @namespace)
        {
            this._namespace = @namespace;
            this._statements = new Collection<CSharpStatement>();
        }

        public CSharpStatementScope BeginScope(string @namespace)
        {
            CSharpStatementScope group = new CSharpStatementScope(@namespace);
            this._statements.Add(group);
            return group;
        }

        public CSharpStatementScope AddSeparator()
        {
            this._statements.Add(new CSharpSeparator());
            return this;
        }

        public CSharpClass AddClass(string name, CSharpModifiers modifiers, params string[] annotations) => this.AddClass(name, modifiers, annotations.AsEnumerable());
        public CSharpClass AddClass(string name, CSharpModifiers modifiers, IEnumerable<string> annotations)
        {
            CSharpClass @class = new CSharpClass(name, modifiers, annotations);
            this._statements.Add(@class);
            return @class;
        }

        public CSharpEnum AddEnum(string name, CSharpModifiers modifiers, IEnumerable<string> annotations)
        {
            CSharpEnum @enum = new CSharpEnum(name, modifiers, annotations);
            this._statements.Add(@enum);
            return @enum;
        }

        public IDisposable CreateRegion(string regionName)
        {
            this._statements.Add(new CSharpRegionStart(regionName));
            return Disposable.Create(() => this._statements.Add(new CSharpRegionEnd()));
        }

        public override void Write(StringWriter writer)
        {
            writer.WriteLine(String.Concat("namespace ", this._namespace))
                .WriteLine("{")
                .PushIndent();

            for (int i = 0; i < this._statements.Count; i++)
            {
                CSharpStatement type = this._statements[i];
                type.Write(writer);
                //if (i + 1 < this._types.Count)
                writer.WriteLine();
            }

            writer.PopIndent()
                .Write("}");
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