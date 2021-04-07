using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoContractClassWriter : DaoWriter
    {
        #region Properties
        public override string LayerName => CodeGeneration.LayerName.DomainModel;
        public override string RegionName => "Contracts";
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Contracts.Any() || model.Statements.Any(x => x.GenerateResultClass);
        public override void Write(DaoCodeGenerationContext context) => ContractCSWriter.Write(context, generateRuntimeSpecifics: true);
        #endregion
    }
}