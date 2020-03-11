using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientContractCSWriter : CodeGenerator
    {
        #region Fields
        private readonly ISchemaRegistry _schemaRegistry;
        #endregion

        #region Constructor
        public ClientContractCSWriter(IErrorReporter errorReporter, ISchemaRegistry schemaRegistry) : base(errorReporter)
        {
            this._schemaRegistry = schemaRegistry;
        }
        #endregion

        #region Overrides
        protected override void Write(StringWriter writer, CodeGenerationModel model)
        {
            CSharpWriter output = new CSharpWriter(writer, model.RootNamespace, Enumerable.Empty<string>());

            DaoCodeGenerationContext context = new DaoCodeGenerationContext(output.Root, null, model, this._schemaRegistry) { Output = output.Root.BeginScope(LayerName.DomainModel) };

            if (model.Contracts.Any())
                ContractCSWriter.Write(context, false);

            output.Generate();
        }
        #endregion
    }
}