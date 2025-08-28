using System.Collections.Generic;

namespace Dibix
{
    internal sealed class ArtifactPackageMetadata
    {
        public IReadOnlyCollection<HttpControllerDefinitionMetadata> Controllers { get; }

        public ArtifactPackageMetadata(IReadOnlyCollection<HttpControllerDefinitionMetadata> controllers)
        {
            Controllers = controllers ?? [];
        }
    }
}