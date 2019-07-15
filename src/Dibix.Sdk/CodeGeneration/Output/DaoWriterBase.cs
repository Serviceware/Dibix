using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class DaoWriterBase : IDaoWriter
    {
        public abstract string RegionName { get; }
        public abstract bool HasContent(OutputConfiguration configuration, SourceArtifacts artifacts);
        public virtual IEnumerable<string> GetGlobalAnnotations(OutputConfiguration configuration) { yield break; }

        void IDaoWriter.Write(DaoWriterContext context)
        {
            HashSet<string> contracts = new HashSet<string>(context.Artifacts.Contracts.Select(x => $"{x.Namespace}.{x.DefinitionName}"));
            this.Write(context, contracts);
        }

        protected abstract void Write(DaoWriterContext context, HashSet<string> contracts);

        protected string PrefixWithRootNamespace(DaoWriterContext context, ContractName contractName, HashSet<string> contracts)
        {
            string resultContractName = contractName.ToString();
            StringBuilder sb = new StringBuilder();
            if (contracts.Contains(resultContractName))
                sb.Append(context.Configuration.Namespace)
                  .Append('.');

            sb.Append(resultContractName);
            return sb.ToString();
        }
    }
}