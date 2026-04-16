using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public abstract class CSharpStatement : CSharpExpression
    {
        private readonly CSharpAnnotationBlock _annotations;

        protected abstract bool MultilineAnnotations { get; }

        protected CSharpStatement() : this(Enumerable.Empty<CSharpAnnotation>()) { }
        protected CSharpStatement(IEnumerable<CSharpAnnotation> annotations)
        {
            _annotations = new CSharpAnnotationBlock(multiline: MultilineAnnotations, annotations);
        }

        public override void Write(StringWriter writer)
        {
            WriteAnnotations(writer);
            WriteBody(writer);
        }

        protected bool WriteAnnotations(StringWriter writer)
        {
            _annotations.Write(writer);
            return !_annotations.IsEmpty;
        }

        protected internal static void WriteIdentifier(StringWriter writer, string identifier)
        {
            writer.WriteIndent();
            WriteIdentifierRaw(writer, identifier);
        }

        protected internal static void WriteIdentifierRaw(StringWriter writer, string identifier)
        {
            if (CSharpKeywords.IsReservedKeyword(identifier))
                writer.WriteRaw('@');

            writer.WriteRaw(identifier);
        }

        protected abstract void WriteBody(StringWriter writer);
    }
}