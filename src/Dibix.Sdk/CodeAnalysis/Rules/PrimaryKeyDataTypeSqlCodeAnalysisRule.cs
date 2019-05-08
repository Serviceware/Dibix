using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class PrimaryKeyDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule<PrimaryKeyDataTypeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 23;
        public override string ErrorMessage => "Only TINYINT/SMALLINT/INT/BIGINT are allowed as primary key: {0}.{1} ({2})";
    }

    public sealed class PrimaryKeyDataTypeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private static readonly HashSet<SqlDataTypeOption> AllowedPrimaryKeyTypes = new HashSet<SqlDataTypeOption>
        {
            SqlDataTypeOption.TinyInt
          , SqlDataTypeOption.SmallInt
          , SqlDataTypeOption.Int
          , SqlDataTypeOption.BigInt
        };
        // helpLine suppressions
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
            "PK_Blob#identifier"
          , "PK_BlobDetail#identifier"
          , "PK_hlbpmusecaseresponseparameters#type"
          , "PK_hlbrerulestorage#partitionkey"
          , "PK_hlbrerulestorage#rowkey"
          , "PK_hlcmdocumentstorage#partitionkey"
          , "PK_hlcmdocumentstorage#rowkey"
          , "PK_hlfrmattrlistitemref#attributepath"
          , "PK_hlfrmattrobjref#attributepath"
          , "PK_hlfrmattrtreeitemref#attributepath"
          , "PK_hlfrmtemplate#content"
          , "PK_hlfrmtemplate#targetattributepath"
          , "PK_hlinffeature#description"
          , "PK_hlnewsdocument#blobid"
          , "PK_hlnewsentrylistattrref#attributekey"
          , "PK_hlnewsentrystrattrref#attributekey"
          , "PK_hlnewsentrystrattrref#value"
          , "PK_hlpedispatchingprocessconfig#componentname"
          , "PK_hlpedispatchingprocessconfig#componentname"
          , "PK_hlpedynamicroutingdefinition#laneid"
          , "PK_hlpedynamicroutingdefinition#processname"
          , "PK_hlpedynamicroutingdefinition#processversion"
          , "PK_hlpeexternalresult#processid"
          , "PK_hlpeexternalresult#taskid"
          , "PK_hlpeinstanceidtowfinstanceid#processinstanceid"
          , "PK_hlpeoverview#instanceid"
          , "PK_hlpeoverviewextensions#domain"
          , "PK_hlpeoverviewextensions#instanceid"
          , "PK_hlpeoverviewextensions#rel"
          , "PK_hlpeprocessaggregatestorage#partitionkey"
          , "PK_hlpeprocessaggregatestorage#rowkey"
          , "PK_hlpeprocessdefinitionroutingtype#processdefinition"
          , "PK_hlpeprocessdefinitionstorage#partitionkey"
          , "PK_hlpeprocessdefinitionstorage#rowkey"
          , "PK_hlpeprocessstate#processinstanceid"
          , "PK_hlpeprocessstate#state"
          , "PK_hlpeprocessstate#taskdefinitionkey"
          , "PK_hlpetablestorage#partitionkey"
          , "PK_hlpetablestorage#rowkey"
          , "PK_hlpetaskdefpersonroutingdata#taskdefinitionkey"
          , "PK_hlpetaskdefroleroutingdata#taskdefinitionkey"
          , "PK_hlpetaskdefteamroutingdata#taskdefinitionkey"
          , "PK_hlpetaskdeftoresponsibility#taskdefinitionkey"
          , "PK_hlpetaskdeftoroutingtype#taskdefinitionkey"
          , "PK_hlpeversiontoprocess#processname"
          , "PK_hlpeversiontoprocess#processversion"
          , "PK_hlpeworkflowdefinitionstorage#partitionkey"
          , "PK_hlpeworkflowdefinitionstorage#rowkey"
          , "PK_hlpmparentprocessidmapping#parentprocessid"
          , "PK_hlpmparentprocessidmapping#processid"
          , "PK_hlpmprocessdependency#processinstanceid"
          , "PK_hlpmprocessdependency#taskaid"
          , "PK_hlpmprocessdependency#taskbid"
          , "PK_hlpmprocessoverview#processinstanceid"
          , "PK_hlpmprocessoverview#taskdefinitionkey"
          , "PK_hlrptslmkpiavailability#timepoint"
          , "PK_hlspextensionsstorage#partitionkey"
          , "PK_hlspextensionsstorage#rowkey"
          , "PK_hlspparentprocattrmapping#attributepath"
          , "PK_hlspparentprocattrmapping#extensionproperty"
          , "PK_hlspparentprocattrmapping#processdefinitionname"
          , "PK_hlspparentprocessidmapping#parentprocessid"
          , "PK_hlspparentprocessidmapping#processid"
          , "PK_hlspprocessdefidtotaskdefkey#processdefinitionidentifier"
          , "PK_hlspprocessdefidtotaskdefkey#taskdefinitionkey"
          , "PK_hlspprocessdefidtowfid#processdefinitionname"
          , "PK_hlspprocessoverview#processinstanceid"
          , "PK_hlsptaskdefkeytolaneid#laneid"
          , "PK_hlsptaskdefkeytolaneid#taskdefinitionkey"
          , "PK_hlsptaskoverview#processinstanceid"
          , "PK_hlsptaskoverview#taskdefinitionkey"
          , "PK_hlstoverview#taskid"
          , "PK_hlstparentprocessidmapping#parentprocessid"
          , "PK_hlstparentprocessidmapping#taskid"
          , "PK_hlststartcontentstorage#PartitionKey"
          , "PK_hlststartcontentstorage#RowKey"
          , "PK_hlsysadhocprocessdefinition#id"
          , "PK_hlsysadhocprocessinstance#instanceid"
          , "PK_hlsysapprovaloptions#keyname"
          , "PK_hlsysapprovements#approvementid"
          , "PK_hlsysassociationhistory#timestamp"
          , "PK_hlsysbaseline#baselineid"
          , "PK_hlsysbaselineassign#baselineid"
          , "PK_hlsysbaselineattr#attrid"
          , "PK_hlsysbaselineattr#baselineid"
          , "PK_hlsysbiconfig#configname"
          , "PK_hlsysbidisplayname#resourcesetname"
          , "PK_hlsysbiemailconfig#configname"
          , "PK_hlsysbikpilastresult#kpiid"
          , "PK_hlsysbimdobj#objectname"
          , "PK_hlsysbimdobj#resourcetypename"
          , "PK_hlsysbimdresset#resourcetypename"
          , "PK_hlsysbimdressetprop#propertyname"
          , "PK_hlsysbimdressetprop#resourcetypename"
          , "PK_hlsysbits#binval"
          , "PK_hlsyschangesqueue#requestid"
          , "PK_hlsyschangessteps#requestid"
          , "PK_hlsysclientevent#clientid"
          , "PK_hlsysclientevent#msgid"
          , "PK_hlsyscticall#fromphone"
          , "PK_hlsyscticall#tophone"
          , "PK_hlsysctiuser#phone"
          , "PK_hlsysdesignobjectdefacl#objectdefname"
          , "PK_hlsysdevicenotificationsubscription#installationid"
          , "PK_hlsysdevicenotificationtemplate#isgrouped"
          , "PK_hlsysdxiprocessdesign#name"
          , "PK_hlsysdxirun#name"
          , "PK_hlsyshlvfs#filepath"
          , "PK_hlsysholidaydate#holidaydate"
          , "PK_hlsysinqueuepending#requestid"
          , "PK_hlsysinqueueprocessed#requestid"
          , "PK_hlsysjoblock#jobid"
          , "PK_hlsyslicexport#id"
          , "PK_hlsyslicimport#id"
          , "PK_hlsysliclicense#lic"
          , "PK_hlsysliclicenseseat#lic"
          , "PK_hlsysliclicenseseat#token"
          , "PK_hlsyslicperseatsessions#sessionid"
          , "PK_hlsysnotificationprocessed#processtime"
          , "PK_hlsysobjectmodelassembly#assemblyname"
          , "PK_hlsysoutqueuepending#requestid"
          , "PK_hlsysoutqueueprocessed#requestid"
          , "PK_hlsysportalconfig#instancename"
          , "PK_hlsysportalconfigurationaudit#audittime"
          , "PK_hlsysprioritymatrix#matrixid"
          , "PK_hlsysprioritymatrixitem#matrixid"
          , "PK_hlsysprjctprcssmp#instanceid"
          , "PK_hlsysprocessconfig#id"
          , "PK_hlsysprocparambool#configid"
          , "PK_hlsysprocparambool#parametername"
          , "PK_hlsysprocparamdatetime#configid"
          , "PK_hlsysprocparamdatetime#parametername"
          , "PK_hlsysprocparamdecimal#configid"
          , "PK_hlsysprocparamdecimal#parametername"
          , "PK_hlsysprocparamint#configid"
          , "PK_hlsysprocparamint#parametername"
          , "PK_hlsysprocparamlargetext#configid"
          , "PK_hlsysprocparamlargetext#parametername"
          , "PK_hlsysprocparamorgunit#configid"
          , "PK_hlsysprocparamorgunit#parametername"
          , "PK_hlsysprocparamperson#configid"
          , "PK_hlsysprocparamperson#parametername"
          , "PK_hlsysprocparamprocrole#configid"
          , "PK_hlsysprocparamprocrole#parametername"
          , "PK_hlsysprocparamproduct#configid"
          , "PK_hlsysprocparamproduct#parametername"
          , "PK_hlsysprocparamtimespan#configid"
          , "PK_hlsysprocparamtimespan#parametername"
          , "PK_hlsyssession#sessionid"
          , "PK_hlsyssessionaddon#sessionidaddon"
          , "PK_hlsysslmagreementtokpi#kpidefinitionid"
          , "PK_hlsysslmcache#cachename"
          , "PK_hlsysslmchange#changeid"
          , "PK_hlsysslmkpidefinition#id"
          , "PK_hlsysslmoptions#name"
          , "PK_hlsysslmprocadhoc#processid"
          , "PK_hlsysslmresponsibility#responsibilityid"
          , "PK_hlsysslmresponsibilitydata#responsibilityid"
          , "PK_hlsysslmresponsibilitymsmq#msmqid"
          , "PK_hlsysslmresponsibilitymsmq#responsibilityid"
          , "PK_hlsysslmservicehourshistory#changedon"
          , "PK_hlsysslmservicetokpi#kpidefinitionid"
          , "PK_hlsysslmslaattrmapping#attributepath"
          , "PK_hlsysslmsvcfaultadhoc#processid"
          , "PK_hlsysslmsvcfaultwf#processid"
          , "PK_hlsysslmsvchoursentryhistory#modifiedon"
          , "PK_hlsysslmsvctoadhocinstance#instanceid"
          , "PK_hlsysslmsvctowfinstance#instanceid"
          , "PK_hlsysslmtablestorage#partitionkey"
          , "PK_hlsysslmtablestorage#rowkey"
          , "PK_hlsysstartpackage#name"
          , "PK_hlsyssvccatcategory#id"
          , "PK_hlsyssvccatcategoryorgunit#categoryid"
          , "PK_hlsyssvccatdocument#id"
          , "PK_hlsyssvccatfeature#id"
          , "PK_hlsyssvccatfeaturebit#featureid"
          , "PK_hlsyssvccatfeaturedate#featureid"
          , "PK_hlsyssvccatfeaturedependency#featureida"
          , "PK_hlsyssvccatfeaturelist#featureid"
          , "PK_hlsyssvccatfeaturelistitem#listitemid"
          , "PK_hlsyssvccatfeaturelistitemdependency#featurelistitemida"
          , "PK_hlsyssvccatfeaturelistitemdependency#featurelistitemidb"
          , "PK_hlsyssvccatfeatureobject#featureid"
          , "PK_hlsyssvccatfeaturetext#featureid"
          , "PK_hlsyssvccatorder#id"
          , "PK_hlsyssvccatorderitem#id"
          , "PK_hlsyssvccatorderitemfeature#feature_id"
          , "PK_hlsyssvccatorderitemfeature#orderitem_id"
          , "PK_hlsyssvccatorderitemfeaturebit#featureid"
          , "PK_hlsyssvccatorderitemfeaturebit#orderitemid"
          , "PK_hlsyssvccatorderitemfeaturedate#featureid"
          , "PK_hlsyssvccatorderitemfeaturedate#orderitemid"
          , "PK_hlsyssvccatorderitemfeaturelist#featureid"
          , "PK_hlsyssvccatorderitemfeaturelist#orderitemid"
          , "PK_hlsyssvccatorderitemfeatureobject#featureid"
          , "PK_hlsyssvccatorderitemfeatureobject#orderitemid"
          , "PK_hlsyssvccatorderitemfeaturetext#featureid"
          , "PK_hlsyssvccatorderitemfeaturetext#orderitemid"
          , "PK_hlsyssvccatpicture#id"
          , "PK_hlsyssvccatproduct#id"
          , "PK_hlsyssvccatproductparent#parentproductid"
          , "PK_hlsyssvccatproductparent#productid"
          , "PK_hlsyssvccatproducttocat#categories_id"
          , "PK_hlsyssvccatproducttocat#products_id"
          , "PK_hlsyssystemtaskhistoryplan#executiontime"
          , "PK_hlsystablecfgdeskfield#usedefault"
          , "PK_hlsystimezoneadjustmentrule#northern_hemisphere"
          , "PK_hlsystweettoprocess#processid"
          , "PK_hlsyswebservice#configurationname"
          , "PK_hlsyswebservice#webserviceclient"
          , "PK_hlsyswrkeffrtprtyassgnmnts#fromdate"
          , "PK_hltmendpointstorage#partitionkey"
          , "PK_hltmendpointstorage#rowkey"
          , "PK_hltmkeytoid#id"
          , "PK_hltmnotificationtaskstorage#partitionkey"
          , "PK_hltmnotificationtaskstorage#rowkey"
          , "PK_hltmroutingitem#roleid"
          , "PK_hltmroutingitem#taskid"
          , "PK_hltmservicetaskstorage#partitionkey"
          , "PK_hltmservicetaskstorage#rowkey"
          , "PK_hltmtaskaggregatelock#id"
          , "PK_hltmtaskaggregatestorage#partitionkey"
          , "PK_hltmtaskaggregatestorage#rowkey"
          , "PK_hltmtaskchangesstorage#partitionkey"
          , "PK_hltmtaskchangesstorage#rowkey"
          , "PK_hltmtaskdatastorage#partitionkey"
          , "PK_hltmtaskdatastorage#rowkey"
          , "PK_hltmtaskdynamicroutingdata#taskid"
          , "PK_hltmtasklistitem#id"
          , "PK_hltmtasklistitemextension#domain"
          , "PK_hltmtasklistitemextension#rel"
          , "PK_hltmtasklistitemextension#taskid"
          , "PK_hltmtaskroleroutingdata#taskid"
          , "PK_hltmtaskrouteddynamically#taskid"
          , "PK_hltmtaskroutedtorole#taskid"
          , "PK_hltmtasksearchdata#id"
          , "PK_hltpparentprocessidmapping#parentprocessid"
          , "PK_hltpparentprocessidmapping#processid"
          , "PK_hltpprocessoverview#processinstanceid"
          , "PK_hltpprocessoverview#taskdefinitionkey"
          , "PK_hlwfactionsubscription#actionname"
          , "PK_hlwfactionsubscription#instanceid"
          , "PK_hlwfactivityproject#assemblyversion"
          , "PK_hlwfactivityproject#projectguid"
          , "PK_hlwfcompletedscope#completedscopeid"
          , "PK_hlwfcompletedscope#instanceid"
          , "PK_hlwfinstance#instanceid"
          , "PK_hlwfnotifconfigdefault#online"
          , "PK_hlwfnotifgroupconfig#online"
          , "PK_hlwfnotifgroupsubscription#online"
          , "PK_hlwfobjectchangedsubscription#queuename"
          , "PK_hlwfobjectchangedsubscription#workflowinstanceid"
          , "PK_hlwfprocessrolesubscription#subscriptionid"
          , "PK_hlwfprocessrolesubscription#wfinstanceid"
          , "PK_hlwfprojectchangedsubscription#queuename"
          , "PK_hlwfprojectchangedsubscription#workflowinstanceid"
          , "PK_hlwfreanalyzesubscription#queuename"
          , "PK_hlwfreanalyzesubscription#workflowinstanceid"
          , "PK_hlwfresprestriction#category"
          , "PK_hlwfresprestriction#responsibilityid"
          , "PK_hlwfrunningactivity#instanceid"
          , "PK_hlwfrunningstagenotifmemo#instanceid"
          , "PK_hlwfrunningstagequeues#instanceid"
        };

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            ICollection<Constraint> constraints = node.Definition.CollectConstraints().ToArray();

            Constraint primaryKey = constraints.SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (primaryKey == null)
                return;

            IDictionary<string, DataTypeReference> columns = node.Definition
                                                                 .ColumnDefinitions
                                                                 .ToDictionary(x => x.ColumnIdentifier.Value, x => x.DataType);

            UniqueConstraintDefinition primaryKeyConstraint = (UniqueConstraintDefinition)primaryKey.Definition;

            string tableName = node.SchemaObjectName.BaseIdentifier.Value;
            foreach (ColumnReference column in primaryKey.Columns)
            {
                // If the PK is not the table's own key, and instead is a FK to a different table's key, no further analysis is needed
                bool hasMatchingForeignKey = constraints.Where(x => x.Type == ConstraintType.ForeignKey)
                                                        .Any(x => x.Columns.Any(y => y.Name == column.Name));
                if (hasMatchingForeignKey)
                    continue;

                string identifier = column.Name;
                if (primaryKeyConstraint.ConstraintIdentifier != null)
                    identifier = String.Concat(primaryKeyConstraint.ConstraintIdentifier.Value, '#', identifier);
                else
                    identifier = String.Concat(tableName, '#', identifier);

                if (columns[column.Name] is SqlDataTypeReference sqlDataType
                 && !AllowedPrimaryKeyTypes.Contains(sqlDataType.SqlDataTypeOption)
                 && !Workarounds.Contains(identifier))
                {
                    base.Fail(column.Hit, tableName, column.Name, sqlDataType.SqlDataTypeOption.ToString().ToUpperInvariant());
                }
            }
        }
    }
}