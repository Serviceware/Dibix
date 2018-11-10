using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration.Lint.Rules
{
    public sealed class SqlSchemaLintRule : SqlLintRule
    {
        public override int Id => 2;
        public override string ErrorMessage => "Missing schema specification";

        public override void ExplicitVisit(SelectStatement node)
        {
            this.CollectWhiteListAndVisit(node);
        }

        public override void ExplicitVisit(InsertStatement node)
        {
            this.CollectWhiteListAndVisit(node);
        }

        public override void ExplicitVisit(MergeStatement node)
        {
            this.CollectWhiteListAndVisit(node);
        }

        public override void ExplicitVisit(UpdateStatement node)
        {
            this.CollectWhiteListAndVisit(node, x => WhiteListDataModificationAlias(x, node.UpdateSpecification));
        }

        public override void ExplicitVisit(DeleteStatement node)
        {
            this.CollectWhiteListAndVisit(node, x => WhiteListDataModificationAlias(x, node.DeleteSpecification));
        }

        private void CollectWhiteListAndVisit(StatementWithCtesAndXmlNamespaces node) { this.CollectWhiteListAndVisit(node, null); }
        private void CollectWhiteListAndVisit(StatementWithCtesAndXmlNamespaces node, Action<ICollection<string>> whiteListRegistrar)
        {
            HashSet<string> whiteList = new HashSet<string>();
            WhiteListCTEExpressions(whiteList, node);
            whiteListRegistrar?.Invoke(whiteList);

            InnerVisitor inner = new InnerVisitor(whiteList, x => base.Fail(x));
            node.Accept(inner);
        }

        private static void WhiteListCTEExpressions(ICollection<string> target, StatementWithCtesAndXmlNamespaces statement)
        {
            if (statement.WithCtesAndXmlNamespaces == null)
                return;

            target.AddRange(statement.WithCtesAndXmlNamespaces.CommonTableExpressions.Select(x => x.ExpressionName.Value));
        }

        private static void WhiteListDataModificationAlias(ICollection<string> whiteList, UpdateDeleteSpecificationBase specification)
        {
            if (specification.FromClause == null)
                return;

            NamedTableReference target = specification.Target as NamedTableReference;
            if (target == null)
                return;

            // UPDATE/DELETE x     -- x         => NamedTableReference with Name: x         => no dbo required
            // FROM dbx_table AS x -- dbx_table => NamedTableReference with Name: dbx_table => dbo required
            bool targetIsAliased = specification.FromClause
                                                .TableReferences
                                                .SelectMany(CollectAliases)
                                                .Any(y => y == target.SchemaObject.BaseIdentifier.Value);

            if (targetIsAliased)
                whiteList.Add(target.SchemaObject.BaseIdentifier.Value);
        }

        private static IEnumerable<string> CollectAliases(TableReference reference)
        {
            if (reference is TableReferenceWithAlias aliasedTable)
            {
                return Enumerable.Repeat(aliasedTable.Alias.Value, 1);
            }

            if (reference is QualifiedJoin join)
            {
                return CollectAliases(join.FirstTableReference)
                .Union(CollectAliases(join.SecondTableReference));
            }

            return Enumerable.Empty<string>();
        }

        private class InnerVisitor : TSqlFragmentVisitor
        {
            private readonly Action<TSqlFragment> _failAction;
            private readonly HashSet<string> _whiteList;

            public InnerVisitor(HashSet<string> whiteList, Action<TSqlFragment> failAction)
            {
                this._failAction = failAction;
                this._whiteList = whiteList;
            }

            public override void ExplicitVisit(NamedTableReference node)
            {
                if (node.SchemaObject.SchemaIdentifier == null && !this._whiteList.Contains(node.SchemaObject.BaseIdentifier.Value))
                    this._failAction(node);
            }
        }
    }
}