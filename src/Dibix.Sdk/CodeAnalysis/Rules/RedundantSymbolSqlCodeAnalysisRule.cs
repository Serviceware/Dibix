using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 38)]
    public sealed class RedundantSymbolSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        // Removing parameters for these matches introduces breaking changes that require a significant amount of work
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            ["hldialogengine_getdialogdesignermetadata@agentid"] = "b78d9bcaee41959b1c2a09bd23e8dc98"
          , ["hldispatching_getdialogdispatchingvalues@agentid"] = "190e6c979e8d5aa5c55443da308a545c"
          , ["hlobjectmanagement_getdialogassociationactivationvalues@agentid"] = "d84b18b9fd91bca88fe638ca0710bf32"
          , ["hlobjectmanagement_getdialogdesignidsbyobjectdefs@parameterkind"] = "c919be0b57d8ba95c4802a028c31d9c3"
          , ["hlsubprocess_getnotificationtasks@fake"] = "e3bccb70ea83839a48cecaae0d06725b"
          , ["hlsubprocess_insertnotificationtemplateattribute@timestamp"] = "ec8d2d37f8b2f9d5e650843123786d10"
          , ["hlsys_refresh_objectsearchdata@forcerefresh"] = "138e44ca08cebbcc49b39cb6d2d92851"
          , ["hlsys_store_configurationmodel@configid"] = "8c94babbeeda916035332362dd11d69d"
          , ["hlsyscal_fn_calculate_difference_formatted@caseid_casedefid"] = "d7d5779c6a6fb6a4436d7b91d6febdff"
          , ["hlsyscal_fn_calculate_token_add@caseid_casedefid"] = "a1fadf36adcbf3757c82925354cdf854"
          , ["hlsysdxi_generate_objecthistory@channelid"] = "30b8d8cf9966b9ce64ec36d02bdd5b41"
          , ["hlsysdxi_generate_objecthistory@starttime"] = "30b8d8cf9966b9ce64ec36d02bdd5b41"
          , ["hlsyssec_inh_pub@defids"] = "fa4443a53337805e80e5853ae6870bc0"
          , ["hlsysum_queryorgunitancestorsass@adminid"] = "a1936208f07861f89018c0689abca38c"
          , ["hlsysum_querypersonancestorsass@adminid"] = "cdca6a415fe5cfa515481753eebd4860"
          , ["hlsysum_queryuserlistagent@filter_agn_set"] = "f9406ea61efb4be208f2ba361649427c"
          , ["hlsysum_queryuserlistperson@filter_per_set"] = "a7ad949864816e21ffdfe14ce0d5cafe"
        };
        private readonly IDictionary<string, DeclareVariableElement> _variables;
        private readonly ICollection<int> _suppressions;

        protected override string ErrorMessageTemplate => "Unused {0}: {1}";

        public RedundantSymbolSqlCodeAnalysisRule()
        {
            this._variables = new Dictionary<string, DeclareVariableElement>();
            this._suppressions = new HashSet<int>();
        }

        protected override void BeginBatch(TSqlBatch node)
        {
            VariableVisitor visitor = new VariableVisitor();
            node.Accept(visitor);
            this._variables.ReplaceWith(visitor.Variables.ToDictionary(x => x.VariableName.Value));
            this._suppressions.ReplaceWith(visitor.ParameterReferences.Where(x => Suppressions.TryGetValue(x.Key, out string hash) && base.Hash == hash).Select(x => x.Value));
        }

        public override void Visit(VariableReference node) => this._variables.Remove(node.Name);

        protected override void EndBatch(TSqlBatch node)
        {
            foreach (DeclareVariableElement unusedVariable in this._variables.Values)
            {
                if (this._suppressions.Contains(unusedVariable.StartOffset))
                    continue;

                base.Fail(unusedVariable, unusedVariable is ProcedureParameter ? "parameter" : "variable", unusedVariable.VariableName.Value);
            }
        }

        private sealed class VariableVisitor : TSqlFragmentVisitor
        {
            public ICollection<DeclareVariableElement> Variables { get; }
            public IDictionary<string, int> ParameterReferences { get; }

            public VariableVisitor()
            {
                this.Variables = new Collection<DeclareVariableElement>();
                this.ParameterReferences = new Dictionary<string, int>();
            }

            public override void Visit(DeclareVariableElement node) => this.Variables.Add(node);
            public override void Visit(FunctionStatementBody node) => this.AddParameterReferences(node.Name, node.Parameters);
            public override void Visit(ProcedureStatementBody node) => this.AddParameterReferences(node.ProcedureReference.Name, node.Parameters);

            private void AddParameterReferences(SchemaObjectName name, IEnumerable<ProcedureParameter> parameters)
            {
                foreach (ProcedureParameter parameter in parameters) 
                    this.ParameterReferences.Add($"{name.BaseIdentifier.Value}{parameter.VariableName.Value}", parameter.StartOffset);
            }
        }
    }
}
