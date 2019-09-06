using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class DaoChildWriterBase : IDaoChildWriter
    {
        public abstract string RegionName { get; }
        public abstract bool HasContent(SourceArtifacts artifacts);
        public virtual IEnumerable<string> GetGlobalAnnotations(OutputConfiguration configuration) { yield break; }

        void IDaoChildWriter.Write(WriterContext context)
        {
            HashSet<string> contracts = new HashSet<string>(context.Artifacts.Contracts.Select(x => $"{x.Namespace}.{x.DefinitionName}"));
            this.Write(context, contracts);
        }

        protected abstract void Write(WriterContext context, HashSet<string> contracts);

        protected string PrefixWithRootNamespace(WriterContext context, ContractName contractName, HashSet<string> contracts)
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