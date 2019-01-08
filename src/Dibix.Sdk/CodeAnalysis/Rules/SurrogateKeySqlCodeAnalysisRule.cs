﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class SurrogateKeySqlCodeAnalysisRule : SqlCodeAnalysisRule<SurrogateKeySqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 24;
        public override string ErrorMessage => "Surrogate keys are only allowed, if a business key is defined: {0}";
    }

    public sealed class SurrogateKeySqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        // helpLine suppressions
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
            "PK_hlbitomattribute(AttributeConfigId)"
          , "PK_hlbitomtable(TableId)"
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
          , "PK_hlsysworkeffort(Id)"
          , "PK_hlsysworkeffortassociation(Id)"
          , "PK_hlsysworkeffortpurpose(Id)"
          , "PK_hlsysworkeffortstatus(Id)"
          , "PK_hlsyswrkeffortpriority(Id)"
          , "PK_hlsyswrkeffrtciassgnmnts(Id)"
          , "PK_hlsyswrkeffrtprtyrle(Id)"
          , "PK_hlsyswrkeffrt_location(Id)"
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

            ICollection<Constraint> constraints = node.Definition.CollectConstraints().ToArray();

            bool hasSurrogateKey = TryGetSurrogateKey(node, constraints, out Constraint primaryKey);
            if (!hasSurrogateKey)
                return;

            // We just assume here that a UQ could be a business key
            bool hasBusinessKey = constraints.Any(x => x.Type == ConstraintType.Unique && !((UniqueConstraintDefinition)x.Definition).IsPrimaryKey);
            if (hasBusinessKey)
                return;

            string identifier = $"({primaryKey.Columns.Single().Name})";
            if (primaryKey.Definition.ConstraintIdentifier != null)
                identifier = String.Concat(primaryKey.Definition.ConstraintIdentifier.Value, identifier);
            else
                identifier = String.Concat(node.SchemaObjectName.BaseIdentifier.Value, identifier);

            if (Workarounds.Contains(identifier))
                return;

            base.Fail(node, node.SchemaObjectName.BaseIdentifier.Value);
        }

        private static bool TryGetSurrogateKey(CreateTableStatement createTableStatement, IEnumerable<Constraint> constraints, out Constraint primaryKey)
        {
            // PK
            primaryKey = constraints.SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (primaryKey == null)
                return false;

            // We only support PK with one column
            if (primaryKey.Columns.Count > 1)
                return false;

            // IDENTITY
            string primaryKeyColumnName = primaryKey.Columns.Single().Name;
            ColumnDefinition primaryKeyColumn = createTableStatement.Definition.ColumnDefinitions.Single(x => x.ColumnIdentifier.Value == primaryKeyColumnName);
            if (primaryKeyColumn.IdentityOptions == null)
                return false;

            return true;
        }
    }
}
 