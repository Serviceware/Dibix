using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public class CSharpStatementScope : CSharpExpression
    {
        private readonly IList<CSharpExpression> _statements;

        public CSharpStatementScope()
        {
            this._statements = new Collection<CSharpExpression>();
        }

        public virtual CSharpStatementScope BeginScope(string @namespace)
        {
            CSharpNamespaceScope group = new CSharpNamespaceScope(@namespace);
            this._statements.Add(group);
            return group;
        }

        public CSharpStatementScope AddSeparator()
        {
            this._statements.Add(new CSharpSeparator());
            return this;
        }

        public CSharpClass AddClass(string name, CSharpModifiers modifiers, params CSharpAnnotation[] annotations) => this.AddClass(name, modifiers, annotations.AsEnumerable());
        public CSharpClass AddClass(string name, CSharpModifiers modifiers, IEnumerable<CSharpAnnotation> annotations)
        {
            CSharpClass @class = new CSharpClass(name, modifiers, annotations);
            this._statements.Add(@class);
            return @class;
        }

        public CSharpInterface AddInterface(string name, CSharpModifiers modifiers)
        {
            CSharpInterface @interface = new CSharpInterface(name, modifiers);
            this._statements.Add(@interface);
            return @interface;
        }

        public CSharpEnum AddEnum(string name, CSharpModifiers modifiers, IEnumerable<CSharpAnnotation> annotations)
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
            for (int i = 0; i < this._statements.Count; i++)
            {
                CSharpExpression expression = this._statements[i];
                expression.Write(writer);

                if (i + 1 < this._statements.Count)
                    writer.WriteLine();
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