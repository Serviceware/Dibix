using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public class CSharpAnnotationBlock : CSharpExpression
    {
        private readonly bool _multiline;
        private readonly IReadOnlyList<CSharpAnnotation> _annotations;

        public bool IsEmpty => _annotations.Count == 0;

        public CSharpAnnotationBlock(bool multiline, IEnumerable<CSharpAnnotation> annotations)
        {
            _annotations = new List<CSharpAnnotation>(annotations.OrderBy(x => x.Length));
            _multiline = multiline;
        }

        public override void Write(StringWriter writer)
        {
            for (int i = 0; i < _annotations.Count; i++)
            {
                CSharpAnnotation annotation = _annotations[i];
                if (_multiline && annotation.WriteIndent)
                    writer.WriteIndent();

                annotation.Write(writer);

                if (_multiline)
                    writer.WriteLine();
                else if (i + 1 == _annotations.Count)
                    writer.WriteRaw(' ');
            }
        }
    }
}