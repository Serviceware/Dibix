using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ClientWriter : ArtifactWriterBase
    {
        public override string LayerName => CodeGeneration.LayerName.Client;

        public override bool HasContent(CodeGenerationModel model) => model.Controllers.Any();

    }
}