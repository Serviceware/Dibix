using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class NamingConventionSqlCodeAnalysisRule : SqlCodeAnalysisRule<NamingConventionSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 17;
        public override string ErrorMessage => "{0}";
    }

    internal static class NamingConvention
    {
#if HELPLINE
        private const string Prefix = "hl";
#else
        private const string Prefix = "dbx";
#endif

        public static readonly string Table                = $"{Prefix}*";
        public static readonly string View                 = $"{Prefix}*vw";
        public static readonly string Type                 = $"{Prefix}*";
        public static readonly string Sequence             = $"SEQ_{Prefix}*";
        public static readonly string Procedure            = $"{Prefix}*";
        public static readonly string Function             = $"{Prefix}*";
        public static readonly string PrimaryKeyConstraint = "PK_<tablename>";
      //public static readonly string ForeignKeyConstraint = "FK_<tablename>_<columnnames>";
        public static readonly string ForeignKeyConstraint = "FK_<tablename>_*";
        public static readonly string CheckConstraint      = "CK_<tablename>_*";
      //public static readonly string UniqueConstraint     = "UQ_<tablename>_<columnnames>";
        public static readonly string UniqueConstraint     = "UQ_<tablename>_*";
        public static readonly string DefaultConstraint    = "DF_<tablename>_<columnname>";
        public static readonly string Index                = "IX_<tablename>_*";
    }

    public sealed class NamingConventionSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        // helpLine suppressions
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
            "PK_hlsysobjecdata" // Renaming this PK would force rebuilding the full text catalog which would be very slow
            
            // Temporary helpLine common blob component workarounds
          , "Blob"
          , "BlobDetail"
          , "FK_BlobDetail_BlobMetaIdentifier"
          , "fnSplit"
          , "spBlobSelect"
          , "spBlobTextSearch"
        };
        private static readonly HashSet<string> ColumnWorkarounds = new HashSet<string>
        {
            "hlsysdxirun#sys_serverexecutionid"
          , "hlsysdxirun#sys_executioninstanceguid"
          , "hlsysdxirun#sys_packagename"
          , "hlsysdxirun#sys_taskname"
          , "hlfrmelementattr#attr_type"
          , "hlfrmelementattrdatetime#attr_type"
          , "hlfrmelementattrlist#attr_type"
          , "hlfrmelementattrstring#attr_type"
          , "hlomdlgrequestactivatedataattr#value_bit"
          , "hlomdlgrequestactivatedataattr#value_int"
          , "hlomdlgrequestactivatedataattr#value_decimal"
          , "hlomdlgrequestactivatedataattr#value_datetime"
          , "hlomdlgrequestactivatedataattr#value_nvarchar"
          , "hlsysattrpathodedef#attrpath_text"
          , "hlsysattrpathodedef#attrpath_lvl"
          , "hlsysattrpathodedef#attrpath_multiple"
          , "hlsysattrpathodedef#attrpath_required"
          , "hlsysattrpathodedef#attrpath_readonly"
          , "hlsysattrpathodedef#attrpath_hidden"
          , "hlsysattrpathodedef#attr_defid"
          , "hlsysattrpathodedef#attr_type"
          , "hlsysattrpathodedef#parent_defid"
          , "hlsysattrpathodedef#parent_type"
          , "hlsysattrpathodedef#gparent_defid"
          , "hlsysattrpathodedef#gparent_type"
          , "hlsysattrpathodedef#ggparent_defid"
          , "hlsysattrpathodedef#ggparent_type"
          , "hlsysbaselineattr#fixedvalue2"
          , "hlsysobjectdefstgcolumn#max_length"
          , "hlsysobjectdefstgcolumn#attrpathattr_defid"
          , "hlsysobjectdefstgcolumn#attrpathattr_type"
          , "hlsysobjectdefstgtable#attrpathattr_defid"
          , "hlsysobjectdefstgtable#attrpathattr_type"
          , "hlsyspathdef#from_objectdefid"
          , "hlsyspathdef#from_objecttype"
          , "hlsyspathdef#to_objectdefid"
          , "hlsyspathdef#to_objecttype"
          , "hlsyspathdef#left_path"
          , "hlsyspathdef#left_output"
          , "hlsyspathdef#left_depth"
          , "hlsyspathdef#left_from_objectdefid"
          , "hlsyspathdef#left_from_objecttype"
          , "hlsyspathdef#left_to_objectdefid"
          , "hlsyspathdef#left_to_objecttype"
          , "hlsyspathdef#middle_objectdefid"
          , "hlsyspathdef#middle_objecttype"
          , "hlsyspathdef#right_path"
          , "hlsyspathdef#right_output"
          , "hlsyspathdef#right_depth"
          , "hlsyspathdef#right_from_objectdefid"
          , "hlsyspathdef#right_from_objecttype"
          , "hlsyspathdef#right_from_objecttype"
          , "hlsyspathdef#right_to_objectdefid"
          , "hlsyspathdef#right_to_objecttype"
          , "hlsysportalcfgcasetablefield#attrpath_odedefid"
          , "hlsysportalcfgcasetablefield#attrpath_attrdefid"
          , "hlsysportalcfgcasetablefield#attrpath_attrtype"
          , "hlsysportalcfgcasetablefield#attrpath_multiple"
          , "hlsysportalcfgcasetablefield#x_cd"
          , "hlsysportalcfgsearchpathorgunit#searchpathorgunit_sectionid"
          , "hlsysportalcfgsearchpathorgunit#searchpathorgunit_sectionname"
          , "hlsysportalcfgsearchpathorgunit#orgunit_sectionid"
          , "hlsysportalcfgsearchpathorgunit#orgunit_sectionname"
          , "hlsysportalcfgsearchpathorgunit#orgunit_objectdefid"
          , "hlsysportalcfgsearchpathorgunit#standardcontact_sectionid"
          , "hlsysportalcfgsearchpathorgunit#standardcontact_sectionname"
          , "hlsysportalcfgsearchpathorgunit#standardcontact_objectdefid"
          , "hlsysportalconfig#attrpathapp1"
          , "hlsysportalconfig#attrpathapp2"
          , "hlsysportalconfig#attrpathacc1"
          , "hlsysportalconfig#attrpathacc2"
          , "hlsysportalconfig#attrpathpassword1"
          , "hlsysportalconfig#attrpathpassword2"
          , "hlsysslmservicehoursentry#time1"
          , "hlsysslmservicehoursentry#time2"
          , "hlsysslmservicehoursentry#dayofweek1"
          , "hlsysslmservicehoursentry#dayofweek2"
          , "hlsysslmservicehoursentry#datetime1"
          , "hlsysslmservicehoursentry#datetime2"
          , "hlsyssvccatfeature#product_id"
          , "hlsyssvccatfeature#hlproduct_objectdefid"
          , "hlsyssvccatfeature#hlproduct_listattributevalue"
          , "hlsyssvccatfeature#hlproduct_version"
          , "hlsyssvccatorderitem#product_id"
          , "hlsyssvccatorderitem#orderitem_id"
          , "hlsyssvccatorderitem#order_id"
          , "hlsyssvccatorderitemfeature#orderitem_id"
          , "hlsyssvccatorderitemfeature#feature_id"
          , "hlsyssvccatpicture#product_id"
          , "hlsyssvccatproducttocat#products_id"
          , "hlsyssvccatproducttocat#categories_id"
          , "hlsystimezoneadjustmentrule#southern_hemisphere"
          , "hlsystimezoneadjustmentrule#northern_hemisphere"
          , "hlsysworkeffort#status_id"
          , "hlsysworkeffort#purpose_id"
          , "hlsysworkeffort#priority_id"
          , "hlsysworkeffortassociation#involvedtorole_id"
          , "hlsysworkeffortassociation#involvedfromrole_id"
          , "hlsyswrkeffrtciassgnmnts#workeffort_id"
          , "hlsyswrkeffrtciassgnmnts#workeffort_id"
          , "hlsyswrkeffrtprtyassgnmnts#workeffort_id"
          , "hlsyswrkeffrtprtyassgnmnts#performer_id"
          , "hlsyswrkeffrtprtyassgnmnts#performer_defid"
          , "hlsyswrkeffrt_tsk#definedin_id"
          , "hlwfrunningactivity#activitystatus_last"
          , "hlwfrunningstagenotifmemo#ntf_wq_m"
          , "hlwfrunningstagenotifmemo#ntf_ct_m"
          , "hlwfrunningstagenotifmemo#ntf_wq_p"
          , "hlwfrunningstagenotifmemo#ntf_ct_p"
          , "hlwfrunningstagenotifmemo#ntf_wq_r"
          , "hlwfrunningstagenotifmemo#ntf_ct_r"
        };

        public override void Visit(CreateTableStatement node)
        {
            if (!node.IsTemporaryTable())
                this.Check(node.SchemaObjectName.BaseIdentifier, nameof(NamingConvention.Table), NamingConvention.Table);

            foreach (ColumnDefinition column in node.Definition.ColumnDefinitions)
            {
                if (ColumnWorkarounds.Contains($"{node.SchemaObjectName.BaseIdentifier.Value}#{column.ColumnIdentifier.Value}"))
                    continue;

                if (!Regex.IsMatch(column.ColumnIdentifier.Value, "^[a-z]+$"))
                    base.Fail(column, $"Column names should be lowercase and contain only characters of the alphabet: {node.SchemaObjectName.BaseIdentifier.Value}.{column.ColumnIdentifier.Value}");
            }
            
            IEnumerable<Constraint> constraints = node.Definition.CollectConstraints();
            foreach (Constraint constraint in constraints)
            {
                this.VisitConstraint(node, constraint);
            }
        }

        public override void Visit(CreateViewStatement node) => this.Check(node.SchemaObjectName.BaseIdentifier, nameof(NamingConvention.View), NamingConvention.View);

        public override void Visit(CreateTypeStatement node) => this.Check(node.Name.BaseIdentifier, nameof(NamingConvention.Type), NamingConvention.Type);

        public override void Visit(CreateSequenceStatement node) => this.Check(node.Name.BaseIdentifier, nameof(NamingConvention.Sequence), NamingConvention.Sequence);
        
        public override void Visit(CreateProcedureStatement node) => this.Check(node.ProcedureReference.Name.BaseIdentifier, nameof(NamingConvention.Procedure), NamingConvention.Procedure);

        public override void Visit(CreateFunctionStatement node) => this.Check(node.Name.BaseIdentifier, nameof(NamingConvention.Function), NamingConvention.Function);

        public override void Visit(CreateIndexStatement node)
        {
            string displayName, pattern;
            if (node.Unique)
            {
                displayName = ConstraintType.Unique.ToDisplayName();
                pattern = NamingConvention.UniqueConstraint;
            }
            else
            {
                displayName = nameof(NamingConvention.Index);
                pattern = NamingConvention.Index;
            }
            this.Check(node.Name, displayName, pattern, new KeyValuePair<string, string>("tablename", node.OnName.BaseIdentifier.Value));
        }

        private void VisitConstraint(CreateTableStatement createTableStatement, Constraint constraint)
        {
            Identifier identifier = constraint.Definition.ConstraintIdentifier;
            if (constraint.Type == ConstraintType.Nullable)
                return;

            if (identifier == null)
                return;

            string pattern = GetNamingConvention(constraint.Type);
            this.Check(identifier, constraint.Type.ToDisplayName(), pattern, ResolveConstraintPlaceholders(createTableStatement, constraint.Columns));
        }

        private void Check(Identifier identifier, string displayName, string pattern, params KeyValuePair<string, string>[] replacements) => this.Check(identifier, displayName, pattern, replacements.AsEnumerable());
        private void Check(Identifier identifier, string displayName, string pattern, IEnumerable<KeyValuePair<string, string>> replacements)
        {
            if (Workarounds.Contains(identifier.Value))
                return;

            string mask = BuildMask(pattern, replacements.ToDictionary(x => x.Key, x => x.Value));
            if (!Regex.IsMatch(identifier.Value, mask))
                base.Fail(identifier, $"{displayName} '{identifier.Value}' does not match naming convention '{pattern}'. Also make sure the name is all lowercase.");
        }

        private static string BuildMask(string pattern, IDictionary<string, string> replacements)
        {
            string OnMatch(Match match)
            {
                if (match.Value == "*")
                    return "[a-z0-9_]+";

                return replacements.TryGetValue(match.Value.TrimStart('<').TrimEnd('>'), out string replacement) ? replacement : match.Value;
            }

            string replacementPattern = String.Join("|", new[] { @"\*" }.Concat(replacements.Keys.Select(x => $@"\<{x}\>")));
            string mask = $"^{Regex.Replace(pattern, replacementPattern, OnMatch)}$";
            return mask;
        }

        private static string GetNamingConvention(ConstraintType constraintType)
        {
            switch (constraintType)
            {
                case ConstraintType.PrimaryKey: return NamingConvention.PrimaryKeyConstraint;
                case ConstraintType.ForeignKey: return NamingConvention.ForeignKeyConstraint;
                case ConstraintType.Unique: return NamingConvention.UniqueConstraint;
                case ConstraintType.Check: return NamingConvention.CheckConstraint;
                case ConstraintType.Default: return NamingConvention.DefaultConstraint;
                default: throw new ArgumentOutOfRangeException(nameof(constraintType), constraintType, null);
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> ResolveConstraintPlaceholders(CreateTableStatement table, IList<ColumnReference> columns)
        {
            yield return new KeyValuePair<string, string>("tablename", table.SchemaObjectName.BaseIdentifier.Value);
            yield return new KeyValuePair<string, string>("columnnames", String.Join(String.Empty, Enumerable.Repeat($"({String.Join("|", table.Definition.ColumnDefinitions.Select(x => x.ColumnIdentifier.Value))})", table.Definition.ColumnDefinitions.Count)));

            if (columns.Count == 1)
                yield return new KeyValuePair<string, string>("columnname", columns[0].Name);
        }
    }
}