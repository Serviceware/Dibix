using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientContractCSWriter : CodeGenerator
    {
        #region Constructor
        public ClientContractCSWriter(IErrorReporter errorReporter) : base(errorReporter) { }
        #endregion

        #region Overrides
        protected override void Write(StringWriter writer, CodeGenerationModel model)
        {
            CSharpWriter output = new CSharpWriter(writer, model.RootNamespace, Enumerable.Empty<string>());

            DaoCodeGenerationContext context = new DaoCodeGenerationContext(output.Root, null, model) { Output = output.Root.BeginScope(LayerName.DomainModel) };

            if (model.Contracts.Any())
                ContractCSWriter.Write(context, false);

            output.Generate();
        }
        #endregion
    }
}