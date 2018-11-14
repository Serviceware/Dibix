using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class SchemaSqlCodeAnalysisRule : SqlCodeAnalysisRule<SchemaSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 2;
        public override string ErrorMessage => "Missing schema specification";
    }

    public sealed class SchemaSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private readonly ICollection<string> _tableAliases = new Collection<string>
        {
            "INSERTED",
            "UPDATED",
            "DELETED"
        };

        public override void Visit(TSqlStatement node)
        {
            ChildAliasVisitor childAliasVisitor = new ChildAliasVisitor();
            node.AcceptChildren(childAliasVisitor);
            this._tableAliases.AddRange(childAliasVisitor.TableAliases);
        }

        public override void Visit(NamedTableReference node)
        {
            if (node.SchemaObject.SchemaIdentifier != null)
                return;

            // Exclude temp tables
            if (node.SchemaObject.BaseIdentifier.Value.Contains("#"))
                return;

            // Exclude aliased tables
            if (this._tableAliases.Any(x => x.Equals(node.SchemaObject.BaseIdentifier.Value, StringComparison.OrdinalIgnoreCase)))
                return;

            base.Fail(node);
        }

        private class ChildAliasVisitor : TSqlFragmentVisitor
        {
            public ICollection<string> TableAliases { get; } = new Collection<string>();

            public override void Visit(TableReferenceWithAlias node)
            {
                if (node.Alias != null)
                    this.TableAliases.Add(node.Alias.Value);
            }

            public override void Visit(CommonTableExpression node)
            {
                this.TableAliases.Add(node.ExpressionName.Value);
            }
        }
    }
}