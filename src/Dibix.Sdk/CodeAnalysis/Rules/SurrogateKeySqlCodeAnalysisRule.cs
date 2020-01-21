using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class SurrogateKeySqlCodeAnalysisRule : SqlCodeAnalysisRule<SurrogateKeySqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 24;
        public override string ErrorMessage => "{0}";
    }

    public sealed class SurrogateKeySqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        // helpLine suppressions
        private static readonly ICollection<string> Workarounds = new HashSet<string>
        {
            "PK_hlbitomattribute(attributeconfigid)"
          , "PK_hlbitomtable(tableid)"
          , "PK_hlbpmattributedefobjectdef(attributedefobjectdefid)"
          , "PK_hlbpmattributedefobjectdefattribute(attributedefobjectdefattributeid)"
          , "PK_hlbpmautomatedtask(automatedtaskid)"
          , "PK_hlbpmautomatedtaskattributedef(automatedtaskattributedefid)"
          , "PK_hlbpmautomatedtaskname(automatedtasknameid)"
          , "PK_hlbpmchangelogitem(changelogitemid)"
          , "PK_hlbpmcontentimageheader(imageid)"
          , "PK_hlbpmendpoint(endpointid)"
          , "PK_hlbpmenvironmentdefinition(environmentdefinitionid)"
          , "PK_hlbpmgenericsetting(genericsettingid)"
          , "PK_hlbpmimportfile(importfileid)"
          , "PK_hlbpmnotificationtask(notificationtaskid)"
          , "PK_hlbpmnotificationtaskcontent(notificationtaskcontentid)"
          , "PK_hlbpmnotifytaskattributedef(notificationtaskattributedefid)"
          , "PK_hlbpmprioritytranslation(prioritytranslationid)"
          , "PK_hlbpmprocessattributedef(processattributedefinitionid)"
          , "PK_hlbpmprocessautomatedtask(processautomatedtask)"
          , "PK_hlbpmprocessdefinition(processdefinitionid)"
          , "PK_hlbpmprocessnotificationtask(processnotificationtask)"
          , "PK_hlbpmprocessshapeproperty(processshapepropertyid)"
          , "PK_hlbpmprocessusecase(processusecaseid)"
          , "PK_hlbpmprocessversion(processversionid)"
          , "PK_hlbpmpropertydefinition(propertydefinitionid)"
          , "PK_hlbpmrange(rangeid)"
          , "PK_hlbpmrule(ruleid)"
          , "PK_hlbpmruleclauserightoption(ruleclauserightoptionid)"
          , "PK_hlbpmruleconditionclause(ruleconditionclauseid)"
          , "PK_hlbpmruleset(rulesetid)"
          , "PK_hlbpmruntimeprocessversion(runtimeprocessversionid)"
          , "PK_hlbpmshape(shapeid)"
          , "PK_hlbpmtranslation(translationid)"
          , "PK_hlbpmusecase(usecaseid)"
          , "PK_hlbpmusecaseattributedef(usecaseattributedefid)"
          , "PK_hlbpmusecasename(usecasenameid)"
          , "PK_hlbpmusecaseparameter(usecaseparameterid)"
          , "PK_hlbpmusecaseresponseparameters(usecaseresponseparameterid)"
          , "PK_hlbpmversionchangelogitem(versionchangelogitemid)"
          , "PK_hlcmhypermedialinks(id)"
          , "PK_hlcmstartcontent(id)"
          , "PK_hlinfdeadletter(id)"
          , "PK_hlnewsentry(entryid)"
          , "PK_hlpetaskcompletionlog(logid)"
          , "PK_hlspnamedassociationusage(id)"
          , "PK_hlsprequiredapprovalrejection(id)"
          , "PK_hlsysdocument(id)"
          , "PK_hlsysdxisetting(id)"
          , "PK_hlsyssecinhtargetsvclogs(logid)"
          , "PK_hlsysservicebrokerlog(logid)"
          , "PK_hlsysslmcntragrmchangeadhoc(id)"
          , "PK_hlsysslmcntragrmchangewf(id)"
          , "PK_hlsystablecfg(tablecfgid)"
          , "PK_hlsysworkeffort(id)"
          , "PK_hlsysworkeffortassociation(id)"
          , "PK_hlsysworkeffortpurpose(id)"
          , "PK_hlsysworkeffortstatus(id)"
          , "PK_hlsyswrkeffortpriority(id)"
          , "PK_hlsyswrkeffrtciassgnmnts(id)"
          , "PK_hlsyswrkeffrtprtyassgnmnts(id,workeffortroleid,fromdate)"
          , "PK_hlsyswrkeffrtprtyrle(id)"
          , "PK_hlsyswrkeffrt_location(id)"
          , "PK_hltmtablecfg(tablecfgid)"
          , "PK_hltmtablecfgcolumn(tablecfgcolumnid)"
          , "PK_hltmtablecfgextcolumn(tablecfgcolumnid)"
          , "PK_hltmtaskerroritem(id)"
          , "PK_hltmtaskreport(id)"
        };

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            IDictionary<string, ColumnDefinition> identityColumns = node.Definition
                                                                        .ColumnDefinitions
                                                                        .Where(x => x.IdentityOptions != null)
                                                                        .ToDictionary(x => x.ColumnIdentifier.Value, StringComparer.OrdinalIgnoreCase);

            ICollection<Constraint> constraints = base.Model.GetConstraints(node.SchemaObjectName).ToArray();

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

            if (Workarounds.Contains(identifier))
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
