using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class DaoWriter
    {
        public abstract string RegionName { get; }
        public abstract string LayerName { get; }
        public abstract bool HasContent(CodeGenerationModel model);
        public virtual IEnumerable<CSharpAnnotation> GetGlobalAnnotations(CodeGenerationModel model) { yield break; }
        public abstract void Write(DaoCodeGenerationContext context);
    }
}