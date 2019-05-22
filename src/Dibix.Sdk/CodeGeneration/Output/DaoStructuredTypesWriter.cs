using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoStructuredTypesWriter : DaoWriter
    {
        protected override IEnumerable<IDaoWriter> SelectWriters()
        {
            yield return new DaoStructuredTypeWriter();
        }
    }
}