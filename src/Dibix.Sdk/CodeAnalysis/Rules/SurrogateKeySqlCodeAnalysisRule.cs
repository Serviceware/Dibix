using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 24)]
    public sealed class SurrogateKeySqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "{0}";

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            IDictionary<string, ColumnDefinition> identityColumns = node.Definition
                                                                        .ColumnDefinitions
                                                                        .Where(x => x.IdentityOptions != null)
                                                                        .ToDictionary(x => x.ColumnIdentifier.Value, StringComparer.OrdinalIgnoreCase);

            ICollection<Constraint> constraints = base.Model.GetTableConstraints(node.SchemaObjectName).ToArray();

            string tableName = node.SchemaObjectName.BaseIdentifier.Value;
            bool hasSurrogateKey = TryGetSurrogateKey(identityColumns.Keys, constraints, out Constraint primaryKey);
            if (!hasSurrogateKey)
            {
                foreach (KeyValuePair<string, ColumnDefinition> identityColumn in identityColumns)
                {
                    base.Fail(identityColumn.Value, $"IDENTITY columns are only allowed for a valid surrogate key: {tableName}.{identityColumn.Key}");
                }
                return;
            }

            Constraint businessKey = constraints.FirstOrDefault(x => IsValidBusinessKey(primaryKey, x));
            if (businessKey != null)
            {
                // If we find this UQ to be valid PK, we suggest making the UQ the PK and replace the surrogate key
                bool isPrimaryKeyCandidate = businessKey.Columns.Count == 1 && PrimaryKeyDataType.AllowedTypes.Contains(businessKey.Columns[0].SqlDataType);
                if (isPrimaryKeyCandidate)
                    base.Fail(node, $"Business key can be used as the primary key and should replace surrogate key: {tableName}");
                
                return;
            }

            string rootIdentifier = primaryKey.Name ?? tableName;
            string identifier = $"{rootIdentifier}({String.Join(",", primaryKey.Columns.Select(x => x.Name))})";

            if (base.IsSuppressed(identifier))
                return;

            base.Fail(node, $"Surrogate keys are only allowed, if a business key is defined: {tableName}");
        }

        private static bool TryGetSurrogateKey(ICollection<string> identityColumns, IEnumerable<Constraint> constraints, out Constraint primaryKey)
        {
            primaryKey = constraints.SingleOrDefault(x => x.Kind == ConstraintKind.PrimaryKey);
            if (primaryKey == null)
                return false;

            bool hasIdentityColumn = primaryKey.Columns.Any(x => identityColumns.Contains(x.Name));
            return hasIdentityColumn;
        }

        private static bool IsValidBusinessKey(Constraint primaryKey, Constraint constraint)
        {
            // We just assume here that a UQ could be a business key
            if (constraint.Kind != ConstraintKind.Unique)
                return false;

            bool businessKeyIsPrimaryKey = primaryKey.Columns.Count == constraint.Columns.Count
                                       && !primaryKey.Columns.Where((x, i) => x.Name != constraint.Columns[i].Name).Any();

            return !businessKeyIsPrimaryKey;
        }
    }
}
