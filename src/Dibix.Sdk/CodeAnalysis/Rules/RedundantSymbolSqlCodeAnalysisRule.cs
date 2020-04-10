using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class RedundantSymbolSqlCodeAnalysisRule : SqlCodeAnalysisRule<RedundantSymbolSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 38;
        public override string ErrorMessage => "Unused {0}: {1}";
    }

    public sealed class RedundantSymbolSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        // Removing parameters for these matches introduces breaking changes that require a significant amount of work
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            ["hlsyscal_fn_calculate_difference_formatted@caseid_casedefid"] = "5c5cb83d3386392dddd085d5d4a46ed3"
          , ["hlsyscal_fn_calculate_token_add@caseid_casedefid"] = "61fd0d726922f50007ec7c97e81474c4"
          , ["hlsyssec_inh_pub@defids"] = "d1f582845d070f3fc8fba010b2ee7cad"
          , ["hlsys_refresh_objectsearchdata@forcerefresh"] = "6b30807267ec1f5388526dea7c6f8e96"
          , ["hlsys_store_configurationmodel@configid"] = "46ea412a438290a60fd02b0de3d187df"
          , ["hlsysdxi_generate_objecthistory@starttime"] = "07797580988d09a56e30131febfaea0b"
          , ["hlsysdxi_generate_objecthistory@channelid"] = "07797580988d09a56e30131febfaea0b"
          , ["hlsysum_queryorgunitancestorsass@adminid"] = "0397613e8cff1ef89eb168911976f5e8"
          , ["hlsysum_querypersonancestorsass@adminid"] = "e33794d3aae53e2f9a0824daad9c3608"
          , ["hlsysum_queryuserlistagent@filter_agn_set"] = "35c2145d478a2d0f1d501e3e0702ccce"
          , ["hlsysum_queryuserlistperson@filter_per_set"] = "4ea1bced43e58c22d56afc951d1bedcf"
          , ["hldialogengine_getdialogdesignermetadata@agentid"] = "5de862d6161493b34950c2f121b2b1bd"
          , ["hldispatching_getdialogdispatchingvalues@agentid"] = "545462c25e37a179bc3e27c1f46cc4b1"
          , ["hlobjectmanagement_getdialogdesignidsbyobjectdefs@parameterkind"] = "c25c7ad7920aee230b017bfc6e2b721d"
          , ["hlobjectmanagement_getdialogassociationactivationvalues@agentid"] = "629672a270ecffb54d06f7b878fcf7fe"
          , ["hlsubprocess_getnotificationtasks@fake"] = "b819c021826ba1774d762a0ff0a6f573"
          , ["hlsubprocess_insertnotificationtemplateattribute@timestamp"] = "b9008bd4555d0239321c5fd0abcb5cc4"
        };
        private readonly IDictionary<string, DeclareVariableElement> _variables;
        private readonly ICollection<int> _suppressions;

        public RedundantSymbolSqlCodeAnalysisRuleVisitor()
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
