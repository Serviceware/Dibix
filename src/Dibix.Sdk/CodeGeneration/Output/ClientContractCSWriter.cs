using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientContractCSWriter : OutputWriter, IWriter
    {
        #region Overrides
        protected override void Write(StringWriter writer, OutputConfiguration configuration, SourceArtifacts artifacts)
        {
            CSharpWriter output = new CSharpWriter(writer, configuration.Namespace, Enumerable.Empty<string>());

            WriterContext context = new WriterContext(output.Root, null, configuration, artifacts, Format);

            if (artifacts.Contracts.Any())
                ContractCSWriter.Write(context, false);

            output.Generate();
        }
        #endregion
    }
}