using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public abstract class CSharpStatement : CSharpExpression
    {
        private readonly IEnumerable<CSharpAnnotation> _annotations;
        
        protected CSharpStatement() : this(Enumerable.Empty<CSharpAnnotation>()) { }
        protected CSharpStatement(IEnumerable<CSharpAnnotation> annotations)
        {
            this._annotations = annotations;
        }

        public override void Write(StringWriter writer)
        {
            IEnumerable<string> annotations = this.CollectAnnotations();
            foreach (string annotation in annotations.OrderBy(x => x.Length))
            {
                this.WriteAnnotation(writer, annotation);
            }

            this.WriteBody(writer);
        }

        protected abstract void WriteBody(StringWriter writer);

        protected virtual void WriteAnnotation(StringWriter writer, string annotation) => writer.WriteLine(annotation);

        private IEnumerable<string> CollectAnnotations()
        {
            foreach (CSharpAnnotation annotation in this._annotations)
            {
                StringWriter writer = new StringWriter();
                annotation.Write(writer);
                yield return writer.ToString();
            }
        }
    }
}