using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 42)]
    public sealed class SetStatementSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private static readonly IDictionary<SetOptions, bool> SupportedOptions = new Dictionary<SetOptions,bool>
        {
            [SetOptions.NoCount] = true
          , [SetOptions.XactAbort] = true
        };
        private string _procedureName;

        protected override string ErrorMessageTemplate => "{0}";

        public override void Visit(PredicateSetStatement node)
        {
            // Validate supported SET statements
            string expression = CollectExpression(node);
            if (!SupportedOptions.TryGetValue(node.Options, out bool on))
            {
                this.ReportUnsupportedSetStatement(node, expression);
                return;
            }

            if (node.IsOn != on)
                this.ReportUnsupportedSetOption(node, on ? "ON" : "OFF", expression);
        }

        public override void Visit(SetCommandStatement node)
        {
            foreach (SetCommand setCommand in node.Commands) 
                Visit(node, setCommand);
        }

        public override void Visit(SetErrorLevelStatement node) => this.ReportUnsupportedSetStatement(node);
        
      //public override void Visit(SetIdentityInsertStatement node) => this.VisitOtherSetStatement(node);
        
        public override void Visit(SetOffsetsStatement node) => this.ReportUnsupportedSetStatement(node);
        
        public override void Visit(SetRowCountStatement node) => this.ReportUnsupportedSetStatement(node);

        public override void Visit(SetStatisticsStatement node) => this.ReportUnsupportedSetStatement(node);
        
        public override void Visit(SetTextSizeStatement node) => this.ReportUnsupportedSetStatement(node);
        
      //public override void Visit(SetTransactionIsolationLevelStatement node) => this.ReportUnsupportedSetStatement(node);

        public override void Visit(SetUserStatement node) => this.ReportUnsupportedSetStatement(node);

        public override void Visit(CreateProcedureStatement node)
        {
            this._procedureName = node.ProcedureReference.Name.BaseIdentifier.Value;

            // Verify that SET XACT_ABORT ON is set when BEGIN TRANSACTION is used without a custom CATCH block that contains ROLLBACK TRANSACTION.
            // This ensures that the transaction is properly rolled back whenever an error occurs.
            XactAbortVisitor xactAbortVisitor = new XactAbortVisitor();
            node.Accept(xactAbortVisitor);
            if (xactAbortVisitor.HasXactAbortOn)
                return;

            // Collect all BEGIN TRANSACTION statements (whether inside TRY..CATCH or not)
            ICollection<BeginTransactionStatement> allBeginTransactionStatements = new Collection<BeginTransactionStatement>();
            BeginTransactionStatementVisitor beginTransactionStatementVisitor = new BeginTransactionStatementVisitor();
            node.Accept(beginTransactionStatementVisitor);
            beginTransactionStatementVisitor.BeginTransactionStatements.Each(allBeginTransactionStatements.Add);

            // Collect TRY..CATCH statements
            ICollection<TryCatchStatementDescriptor> tryCatchStatements = new Collection<TryCatchStatementDescriptor>();
            TryCatchStatementVisitor tryCatchStatementVisitor = new TryCatchStatementVisitor(parent: null, tryCatchStatements);
            node.Accept(tryCatchStatementVisitor);

            // Walk each TRY..CATCH statement bottom up
            ICollection<TryCatchStatementDescriptor> nestedTryCatchStatements = tryCatchStatements.Where(x => x.IsLeaf).ToArray();
            ICollection<BeginTransactionStatementDescriptor> currentBeginTransactionStatements = new HashSet<BeginTransactionStatementDescriptor>();
            foreach (TryCatchStatementDescriptor tryCatchStatementDescriptor in nestedTryCatchStatements)
            {
                VisitTryCatch(allBeginTransactionStatements, currentBeginTransactionStatements, tryCatchStatementDescriptor);
            }

            // Populate errors for remaining violations
            foreach (BeginTransactionStatement beginTransactionStatement in allBeginTransactionStatements)
            {
                base.Fail(beginTransactionStatement, "SET XACT_ABORT ON should be set when working with BEGIN TRANSACTION without a custom TRY..CATCH block to ensure the transaction is rolled back in case of an error");
            }
        }

        private void Visit(TSqlFragment node, SetCommand command)
        {
            switch (command)
            {
                case GeneralSetCommand generalSetCommand:
                    this.Visit(node, generalSetCommand);
                    break;

                default:
                    this.ReportUnsupportedSetStatement(node);
                    break;
            }
        }

        private void Visit(TSqlFragment node, GeneralSetCommand generalSetCommand)
        {
            switch (generalSetCommand.CommandType)
            {
                // SET DEADLOCK_PRIORITY LOW
                case GeneralSetCommandType.DeadlockPriority:
                    if (!(generalSetCommand.Parameter is IdentifierLiteral identifierLiteral))
                        return; // ???

                    const string expectedDeadlockPriority = "LOW";
                    if (!String.Equals(identifierLiteral.Value, expectedDeadlockPriority, StringComparison.OrdinalIgnoreCase)) 
                        this.ReportUnsupportedSetOption(node, expectedDeadlockPriority);

                    break;
                    
                // SET CONTEXT_INFO
                case GeneralSetCommandType.ContextInfo:
                    break;

                // SET DATEFORMAT MDY
                case GeneralSetCommandType.DateFormat:
                    string expression = CollectExpression(node);
                    base.FailIfUnsuppressed(node, this._procedureName, BuildUnsupportedSetStatementMessage(expression));
                    break;

                default:
                    this.ReportUnsupportedSetStatement(node);
                    break;
            }
        }

        private void ReportUnsupportedSetStatement(TSqlFragment node)
        {
            string expression = CollectExpression(node);
            this.ReportUnsupportedSetStatement(node, expression);
        }
        private void ReportUnsupportedSetStatement(TSqlFragment node, string expression) => base.Fail(node, BuildUnsupportedSetStatementMessage(expression));

        private static string BuildUnsupportedSetStatementMessage(string expression) => $"Unsupported SET statement: {expression}";

        private void ReportUnsupportedSetOption(TSqlFragment node, string expectedOption)
        {
            string expression = CollectExpression(node);
            this.ReportUnsupportedSetOption(node, expectedOption, expression);
        }
        private void ReportUnsupportedSetOption(TSqlFragment node, string expectedOption, string expression) => base.Fail(node, $"Only {expectedOption} is supported for SET statement: {expression}");

        private static string CollectExpression(TSqlFragment node) => Regex.Replace(node.Dump(), @"\s{2,}", " ");

        private static void VisitTryCatch(ICollection<BeginTransactionStatement> allBeginTransactionStatements, ICollection<BeginTransactionStatementDescriptor> currentBeginTransactionStatements, TryCatchStatementDescriptor tryCatchStatementDescriptor)
        {
            TryCatchStatementDescriptor current = tryCatchStatementDescriptor;
            do
            {
                // Collect BEGIN TRANSACTION within TRY
                BeginTransactionStatementVisitor beginTransactionStatementVisitor = new BeginTransactionStatementVisitor();
                current.Statement.TryStatements.Accept(beginTransactionStatementVisitor);
                beginTransactionStatementVisitor.BeginTransactionStatements.Each(x => currentBeginTransactionStatements.Add(new BeginTransactionStatementDescriptor(x)));

                // Collect THROW statement within CATCH
                ThrowStatementVisitor throwStatementVisitor = new ThrowStatementVisitor();
                current.Statement.CatchStatements.Accept(throwStatementVisitor);

                // Collect ROLLBACK statement within CATCH
                RollbackTransactionStatementVisitor rollbackTransactionStatementVisitor = new RollbackTransactionStatementVisitor();
                current.Statement.CatchStatements.Accept(rollbackTransactionStatementVisitor);

                // If the CATCH contains ROLLBACK, all nested BEGIN TRANSACTION statements, can be treated as rolled back, if there was no CATCH block that ignored the error
                if (rollbackTransactionStatementVisitor.HasRollback)
                {
                    // If there was a nested CATCH block without rethrow, the parent CATCH containing ROLLBACK won't be hit,
                    // The affected BEGIN TRANSACTION statements in this case, can't be treated as rolled back and are marked as Keep = true.
                    currentBeginTransactionStatements.Where(x => !x.Keep).Each(x => allBeginTransactionStatements.Remove(x.Statement));
                    currentBeginTransactionStatements.Clear();
                }

                // If the CATCH did not rethrow, the error is ignored and not bubbled up.
                // Therefore all BEGIN TRANSACTION statements in the TRY block won't be rolled back.
                if (!throwStatementVisitor.HasThrow)
                    currentBeginTransactionStatements.Each(x => x.Keep = true);

                current = current.Parent;
            } while (current != null);
        }

        private sealed class BeginTransactionStatementVisitor : TSqlFragmentVisitor
        {
            public ICollection<BeginTransactionStatement> BeginTransactionStatements { get; } = new Collection<BeginTransactionStatement>();

            public override void ExplicitVisit(BeginTransactionStatement node)
            {
                this.BeginTransactionStatements.Add(node);
            }
        }

        private sealed class ThrowStatementVisitor : TSqlFragmentVisitor
        {
            public bool HasThrow { get; private set; }

            public override void ExplicitVisit(ThrowStatement node)
            {
                this.HasThrow = true;
            }
        }

        private sealed class RollbackTransactionStatementVisitor : TSqlFragmentVisitor
        {
            public bool HasRollback { get; private set; }

            public override void ExplicitVisit(RollbackTransactionStatement node)
            {
                this.HasRollback = true;
            }
        }

        private sealed class XactAbortVisitor : TSqlFragmentVisitor
        {
            public bool HasXactAbortOn { get; private set; }

            public override void Visit(PredicateSetStatement node)
            {
                this.HasXactAbortOn = node.Options == SetOptions.XactAbort && node.IsOn;
            }
        }

        private sealed class TryCatchStatementVisitor : TSqlFragmentVisitor
        {
            private readonly TryCatchStatementDescriptor _parent;
            private readonly ICollection<TryCatchStatementDescriptor> _target;

            public bool HasMatch { get; private set; }

            public TryCatchStatementVisitor(TryCatchStatementDescriptor parent, ICollection<TryCatchStatementDescriptor> target)
            {
                this._parent = parent;
                this._target = target;
            }

            public override void ExplicitVisit(TryCatchStatement node)
            {
                this.HasMatch = true;

                TryCatchStatementDescriptor descriptor = new TryCatchStatementDescriptor(node);
                descriptor.Parent = this._parent;
                this._target.Add(descriptor);

                TryCatchStatementVisitor visitor = new TryCatchStatementVisitor(descriptor, this._target);
                node.TryStatements.Accept(visitor);
                node.CatchStatements.Accept(visitor);
                descriptor.IsLeaf = !visitor.HasMatch;
            }
        }

        private sealed class TryCatchStatementDescriptor
        {
            public TryCatchStatement Statement { get; }
            public TryCatchStatementDescriptor Parent { get; set; }
            public bool IsLeaf { get; set; }

            public TryCatchStatementDescriptor(TryCatchStatement statement)
            {
                this.Statement = statement;
            }
        }

        private sealed class BeginTransactionStatementDescriptor
        {
            public BeginTransactionStatement Statement { get; }
            public bool Keep { get; set; }

            public BeginTransactionStatementDescriptor(BeginTransactionStatement statement)
            {
                this.Statement = statement;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is BeginTransactionStatementDescriptor other && Equals(other);
            }

            public override int GetHashCode()
            {
                return this.Statement.GetHashCode();
            }

            private bool Equals(BeginTransactionStatementDescriptor other)
            {
                return this.Statement.Equals(other.Statement);
            }
        }
    }
}