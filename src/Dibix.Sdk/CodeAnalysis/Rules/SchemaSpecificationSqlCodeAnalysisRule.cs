using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class SchemaSpecificationSqlCodeAnalysisRule : SqlCodeAnalysisRule<SchemaSpecificationSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 2;
        public override string ErrorMessage => "Missing schema specification";
    }

    public sealed class SchemaSpecificationSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private readonly ICollection<string> _tableAliases = new Collection<string>
        {
            "INSERTED",
            "UPDATED",
            "DELETED"
        };

        // Visit whole statement before this visitor
        public override void ExplicitVisit(TSqlScript node)
        {
            ChildAliasVisitor childAliasVisitor = new ChildAliasVisitor();
            node.AcceptChildren(childAliasVisitor);
            this._tableAliases.AddRange(childAliasVisitor.TableAliases);

            base.ExplicitVisit(node);
        }

        public override void Visit(CreateTableStatement node)
        {
            this.Check(node.SchemaObjectName);
        }

        public override void Visit(NamedTableReference node)
        {
            this.Check(node.SchemaObject);
        }

        private void Check(SchemaObjectName name)
        {
            if (name.SchemaIdentifier != null)
                return;

            // Exclude temp tables
            if (name.BaseIdentifier.Value.Contains("#"))
                return;

            // Exclude aliased tables
            if (this._tableAliases.Any(x => x.Equals(name.BaseIdentifier.Value, StringComparison.OrdinalIgnoreCase)))
                return;

            base.Fail(name);
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