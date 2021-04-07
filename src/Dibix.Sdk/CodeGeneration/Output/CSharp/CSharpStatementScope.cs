using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public class CSharpStatementScope : CSharpStatement
    {
        private readonly string _namespace;
        private readonly IList<CSharpExpression> _statements;

        protected CSharpStatementScope(string @namespace) : this(@namespace, Enumerable.Empty<CSharpAnnotation>()) { }
        protected CSharpStatementScope(string @namespace, IEnumerable<CSharpAnnotation> globalAnnotations) : base(MarkAnnotationsAsGlobal(globalAnnotations))
        {
            this._namespace = @namespace;
            this._statements = new Collection<CSharpExpression>();
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

        public CSharpClass AddClass(string name, CSharpModifiers modifiers, params CSharpAnnotation[] annotations) => this.AddClass(name, modifiers, annotations.AsEnumerable());
        public CSharpClass AddClass(string name, CSharpModifiers modifiers, IEnumerable<CSharpAnnotation> annotations)
        {
            CSharpClass @class = new CSharpClass(name, modifiers, annotations);
            this._statements.Add(@class);
            return @class;
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

        protected override void WriteBody(StringWriter writer)
        {
            writer.WriteLine(String.Concat("namespace ", this._namespace))
                .WriteLine("{")
                .PushIndent();

            foreach (CSharpExpression expression in this._statements)
            {
                expression.Write(writer);
                writer.WriteLine();
            }

            writer.PopIndent()
                .Write("}");
        }

        private static IEnumerable<CSharpAnnotation> MarkAnnotationsAsGlobal(IEnumerable<CSharpAnnotation> globalAnnotations)
        {
            IEnumerable<CSharpAnnotation> globalAnnotationsEnumerated = globalAnnotations as ICollection<CSharpAnnotation> ?? globalAnnotations.ToArray();
            globalAnnotationsEnumerated.Each(x => x.IsGlobal = true);
            return globalAnnotationsEnumerated;
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