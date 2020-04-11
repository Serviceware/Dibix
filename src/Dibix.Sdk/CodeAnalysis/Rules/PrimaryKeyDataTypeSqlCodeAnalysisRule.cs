using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Column = Dibix.Sdk.Sql.Column;
using TableType = Dibix.Sdk.Sql.TableType;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class PrimaryKeyDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule<PrimaryKeyDataTypeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 23;
        public override string ErrorMessage => "Only TINYINT/SMALLINT/INT/BIGINT are allowed as primary key: {0}.{1} ({2})";
    }

    public sealed class PrimaryKeyDataTypeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        // helpLine suppressions
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            ["PK_Blob#identifier"] = "c1a9c74d497321295a1ed6a008416ced"
          , ["PK_BlobDetail#identifier"] = "3f6d43d6d559c87c6a4ef26086a565f8"
          , ["PK_hlbrerulestorage#partitionkey"] = "428f925ad323236239f40c0fe3191529"
          , ["PK_hlbrerulestorage#rowkey"] = "428f925ad323236239f40c0fe3191529"
          , ["PK_hlcmdocumentstorage#partitionkey"] = "1f362a4c866eff2af9f70255c528e53b"
          , ["PK_hlcmdocumentstorage#rowkey"] = "1f362a4c866eff2af9f70255c528e53b"
          , ["PK_hlfrmattrlistitemref#attributepath"] = "ec2600dde7702921cb9ab2c4c3f8a63e"
          , ["PK_hlfrmattrobjref#attributepath"] = "5584a157683f25225340309b710bdc8b"
          , ["PK_hlfrmattrtreeitemref#attributepath"] = "b7275e6c6729d563a86c52fec06defa1"
          , ["PK_hlfrmtemplate#targetattributepath"] = "6f1cff07fd5eca22e0ba54caa614141e"
          , ["PK_hlnewsdocument#blobid"] = "ef2283bf862ea8a02979ff218067bc42"
          , ["PK_hlnewsentrylistattrref#attributekey"] = "36e8c322f40c7ea969821b7a4b3d5ce6"
          , ["PK_hlnewsentrystrattrref#attributekey"] = "e63d8871bbdb70d5af835fefe25d657f"
          , ["PK_hlnewsentrystrattrref#value"] = "e63d8871bbdb70d5af835fefe25d657f"
          , ["PK_hlpeexternalresult#processid"] = "4086cb92960e3f69187e0768fdf6822e"
          , ["PK_hlpeexternalresult#taskid"] = "4086cb92960e3f69187e0768fdf6822e"
          , ["PK_hlpeinstanceidtowfinstanceid#processinstanceid"] = "2deec69771e99ec85c5c7137dea6a4a9"
          , ["PK_hlpeoverview#instanceid"] = "4174532b9091413fd716f9cbf14a5534"
          , ["PK_hlpeoverviewextensions#domain"] = "31250842cba25686cb52f7bb1d2d20b3"
          , ["PK_hlpeoverviewextensions#instanceid"] = "31250842cba25686cb52f7bb1d2d20b3"
          , ["PK_hlpeoverviewextensions#rel"] = "31250842cba25686cb52f7bb1d2d20b3"
          , ["PK_hlpeprocessaggregatestorage#partitionkey"] = "c06e020e343cc2fc02befbf2bacff720"
          , ["PK_hlpeprocessaggregatestorage#rowkey"] = "c06e020e343cc2fc02befbf2bacff720"
          , ["PK_hlpeprocessdefinitionroutingtype#processdefinition"] = "5d6ec40e3ac81ee8167fcaf3ae7e4ba3"
          , ["PK_hlpeprocessdefinitionstorage#partitionkey"] = "797c037090dd7675d1b3e39cf71a81d7"
          , ["PK_hlpeprocessdefinitionstorage#rowkey"] = "797c037090dd7675d1b3e39cf71a81d7"
          , ["PK_hlpeprocessstate#processinstanceid"] = "43562d14e0fa31d422e5341dc0d975cc"
          , ["PK_hlpeprocessstate#taskdefinitionkey"] = "43562d14e0fa31d422e5341dc0d975cc"
          , ["PK_hlpetablestorage#partitionkey"] = "35fd0ba3d41616171cc2fd5f767d87b7"
          , ["PK_hlpetablestorage#rowkey"] = "35fd0ba3d41616171cc2fd5f767d87b7"
          , ["PK_hlpetaskdefpersonroutingdata#taskdefinitionkey"] = "42b95657e6f3686e8bf6548fd18e49a8"
          , ["PK_hlpetaskdefroleroutingdata#taskdefinitionkey"] = "62f9fb3773964b77ef784733349de209"
          , ["PK_hlpetaskdefteamroutingdata#taskdefinitionkey"] = "9c75af13662c1b3f660cc47fa112d3bb"
          , ["PK_hlpetaskdeftoroutingtype#taskdefinitionkey"] = "43903b8c9c6f365387206327731fa423"
          , ["PK_hlpeversiontoprocess#processname"] = "0c50ca2fd7307de4348d13b67f3433a7"
          , ["PK_hlpeversiontoprocess#processversion"] = "0c50ca2fd7307de4348d13b67f3433a7"
          , ["PK_hlpeworkflowdefinitionstorage#partitionkey"] = "a7ee3549b5508bb895f071a9e12190f0"
          , ["PK_hlpeworkflowdefinitionstorage#rowkey"] = "a7ee3549b5508bb895f071a9e12190f0"
          , ["PK_hlpmparentprocessidmapping#parentprocessid"] = "08ed77e65f5ed1c281823bafbfd7ea12"
          , ["PK_hlpmparentprocessidmapping#processid"] = "08ed77e65f5ed1c281823bafbfd7ea12"
          , ["PK_hlpmprocessoverview#processinstanceid"] = "4bd9be4f528312c5e7cf5089df7fa2ce"
          , ["PK_hlpmprocessoverview#taskdefinitionkey"] = "4bd9be4f528312c5e7cf5089df7fa2ce"
          , ["PK_hlrptslmkpiavailability#timepoint"] = "7d9fdf4dbe5684e7ef5ca54039eaee11"
          , ["PK_hlspextensionsstorage#partitionkey"] = "b29692741a51abcd80c25d8cb9a9b309"
          , ["PK_hlspextensionsstorage#rowkey"] = "b29692741a51abcd80c25d8cb9a9b309"
          , ["PK_hlspparentprocattrmapping#attributepath"] = "849a02e49f6cfb93a812208afee0d1ea"
          , ["PK_hlspparentprocattrmapping#extensionproperty"] = "849a02e49f6cfb93a812208afee0d1ea"
          , ["PK_hlspparentprocattrmapping#processdefinitionname"] = "849a02e49f6cfb93a812208afee0d1ea"
          , ["PK_hlspparentprocessidmapping#parentprocessid"] = "4d8932c1e7f87462792743fea367647b"
          , ["PK_hlspparentprocessidmapping#processid"] = "4d8932c1e7f87462792743fea367647b"
          , ["PK_hlspprocessdefidtotaskdefkey#processdefinitionidentifier"] = "32aee06dd433a77eccab59993bfc9ca1"
          , ["PK_hlspprocessdefidtotaskdefkey#taskdefinitionkey"] = "32aee06dd433a77eccab59993bfc9ca1"
          , ["PK_hlspprocessdefidtowfid#processdefinitionname"] = "1d29b761085aeef0d3845a5b2d49126a"
          , ["PK_hlspprocessoverview#processinstanceid"] = "a893282fab649267675357af042f114a"
          , ["PK_hlsptaskdefkeytolaneid#laneid"] = "0b583a907db09249de3be72d54f19624"
          , ["PK_hlsptaskdefkeytolaneid#taskdefinitionkey"] = "0b583a907db09249de3be72d54f19624"
          , ["PK_hlsptaskoverview#taskdefinitionkey"] = "6348a08a88e3e7f5208ae0a6c7086f87"
          , ["PK_hlstoverview#taskid"] = "c0ebf97c1ba5220437e65e2224e79c97"
          , ["PK_hlstparentprocessidmapping#parentprocessid"] = "2b6f53ebce335198e22ecc615c3d6fae"
          , ["PK_hlstparentprocessidmapping#taskid"] = "2b6f53ebce335198e22ecc615c3d6fae"
          , ["PK_hlsysadhocprocessdefinition#id"] = "120c950250ef6027a1db87511d72cba3"
          , ["PK_hlsysadhocprocessinstance#instanceid"] = "ff4fd47c52d6b5462d96ff7e5c15a3b6"
          , ["PK_hlsysapprovaloptions#keyname"] = "2f72c82477272e6a8289e36c84a2b51c"
          , ["PK_hlsysapprovements#approvementid"] = "ab66bb998a7484aecde144c5382d2714"
          , ["PK_hlsysbaseline#baselineid"] = "2d04882600d349fc389a5163fb678f89"
          , ["PK_hlsysbaselineattr#attrid"] = "12df732863664d9e5e27f81f1e78c669"
          , ["PK_hlsysbiconfig#configname"] = "535a556d50e92c6cc177d8d89a24df8a"
          , ["PK_hlsysbidisplayname#resourcesetname"] = "66647e220b100d3bf7f2db4a1639aff7"
          , ["PK_hlsysbiemailconfig#configname"] = "16d72fe3d27942523f8c4fcd0bfde0b7"
          , ["PK_hlsysbikpilastresult#kpiid"] = "e44424d929ccac466c4e872320ce3b02"
          , ["PK_hlsysbimdobj#objectname"] = "08bf3a537abafa56e591e68f70277f0a"
          , ["PK_hlsysbimdresset#resourcetypename"] = "1af1255bee44e03a79a6d8e4dba37f91"
          , ["PK_hlsysbimdressetprop#propertyname"] = "b898d4ae8b456a7069e296ffef1b8986"
          , ["PK_hlsysbits#binval"] = "9d8211bfd44e51f0d1c43eab5f814901"
          , ["PK_hlsyschangesqueue#requestid"] = "de56c87491c3742abe27f4775d42bb69"
          , ["PK_hlsysclientevent#clientid"] = "22290af9c3e9ae9fed584fa6d384f54a"
          , ["PK_hlsysclientevent#msgid"] = "22290af9c3e9ae9fed584fa6d384f54a"
          , ["PK_hlsyscticall#fromphone"] = "2ac90c6d1a524a96439d3555ed702a06"
          , ["PK_hlsyscticall#tophone"] = "2ac90c6d1a524a96439d3555ed702a06"
          , ["PK_hlsysctiuser#phone"] = "840ecd3c6d47e9c47492cfbfa6baf613"
          , ["PK_hlsysdesignobjectdefacl#objectdefname"] = "9b6625fae943f76ae67cfb138083f26a"
          , ["PK_hlsysdevicenotificationsubscription#installationid"] = "f6abde998667214cf12fd6c9eb11973d"
          , ["PK_hlsysdevicenotificationtemplate#isgrouped"] = "9038d45988c5b16f392fdc414b209f3b"
          , ["PK_hlsyshlvfs#filepath"] = "8bf2106e5a2ca08cc54eb423a2fdf8d0"
          , ["PK_hlsysinqueuepending#requestid"] = "0de0840d4ca13f58ef3f2c842b33b251"
          , ["PK_hlsysinqueueprocessed#requestid"] = "663a840a08e1f76779ae07d50a995a54"
          , ["PK_hlsysjoblock#jobid"] = "9d4508338690bbe85caf7f315caa6372"
          , ["PK_hlsyslicexport#id"] = "b18c61be88e2e53bb06803aa3c6e6b8b"
          , ["PK_hlsyslicimport#id"] = "d91cd86a602ce0cf8a0ddd279bf9847f"
          , ["PK_hlsysliclicense#lic"] = "ff8e1ded6b402a05020b5f1f6ec1c810"
          , ["PK_hlsysliclicenseseat#lic"] = "001bd6b5b6ec2d725371afd514362747"
          , ["PK_hlsysliclicenseseat#token"] = "001bd6b5b6ec2d725371afd514362747"
          , ["PK_hlsyslicperseatsessions#sessionid"] = "61eb8990bd45d88b24c442c33a0825f9"
          , ["PK_hlsysnotificationprocessed#processtime"] = "caa909f2fc71b01bc3ea2ddcbba6561c"
          , ["PK_hlsysobjectmodelassembly#assemblyname"] = "4ecedad6898ce02052f02519673c1aa6"
          , ["PK_hlsysoutqueuepending#requestid"] = "c44d90a44b80933e65ba7de2df5b9e67"
          , ["PK_hlsysoutqueueprocessed#requestid"] = "ad3c052ca95efc66a41a361351b2945b"
          , ["PK_hlsysportalconfig#instancename"] = "d3ba37ada7794db37d71df4b87547128"
          , ["PK_hlsysportalconfigurationaudit#audittime"] = "bb1e2f257ad1704d5f260647a1c0df91"
          , ["PK_hlsysprioritymatrix#matrixid"] = "6500663a9e69bf0c7b139af35afb01b2"
          , ["PK_hlsysprjctprcssmp#instanceid"] = "b3165911a50585bf4b0ca72c362e0212"
          , ["PK_hlsysprocessconfig#id"] = "c53bdaacd7bddd3b64b7d7244eff0bd3"
          , ["PK_hlsysprocparambool#parametername"] = "df415a441d91f20114ec5ab8d6fa51cc"
          , ["PK_hlsysprocparamdatetime#parametername"] = "0b4d7858d21b7dbcb6408c1a3f9575f4"
          , ["PK_hlsysprocparamdecimal#parametername"] = "66eee381100547f246ccc21d5031029c"
          , ["PK_hlsysprocparamint#parametername"] = "c630b04d0d752e852cc413049852c2e3"
          , ["PK_hlsysprocparamlargetext#parametername"] = "e099c75253391718fd6f9becc367d17d"
          , ["PK_hlsysprocparamorgunit#parametername"] = "7c08f2f4d85758b66216a7c9ea42bd3c"
          , ["PK_hlsysprocparamperson#parametername"] = "42cde66b5b9f0d2b5b7fce25ff33d367"
          , ["PK_hlsysprocparamprocrole#parametername"] = "3a905be5fb6df584b2d502737ec69b72"
          , ["PK_hlsysprocparamproduct#parametername"] = "aafd2f021e6cf441a39eaa86f5baf7bd"
          , ["PK_hlsysprocparamtimespan#parametername"] = "086b607a5ef070b0ab5e7b9cca4d56fe"
          , ["PK_hlsyssession#sessionid"] = "b7573701ae1508be638262f87798b71e"
          , ["PK_hlsyssessionaddon#sessionidaddon"] = "88dcc6257598d09c687c1c3f7d22db52"
          , ["PK_hlsysslmcache#cachename"] = "ae80fbc36790710947f6b791c4880529"
          , ["PK_hlsysslmchange#changeid"] = "e92d4fd70e086178859952574759fb53"
          , ["PK_hlsysslmkpidefinition#id"] = "73fad037bd3885b34405e7a07721a2dd"
          , ["PK_hlsysslmoptions#name"] = "a2b854d2d9f774d962d21b610ba528cd"
          , ["PK_hlsysslmprocadhoc#processid"] = "8a869661c1a9fffa6669b81765e852e8"
          , ["PK_hlsysslmresponsibility#responsibilityid"] = "d56d543974d55808ec6498f4bc61f541"
          , ["PK_hlsysslmresponsibilitymsmq#msmqid"] = "785ca53febfad32e87302eadd06f5d55"
          , ["PK_hlsysslmservicehourshistory#changedon"] = "7854d4b4a6131bf8fe5b23039b5d8b39"
          , ["PK_hlsysslmslaattrmapping#attributepath"] = "92c0f8fd70531233620957c33e0e5c79"
          , ["PK_hlsysslmsvchoursentryhistory#modifiedon"] = "87f4b7a5fe8b631cc53c50a2438cb8a0"
          , ["PK_hlsysslmtablestorage#partitionkey"] = "ce4aaedda8e6ad59b80218891ffc15f1"
          , ["PK_hlsysslmtablestorage#rowkey"] = "ce4aaedda8e6ad59b80218891ffc15f1"
          , ["PK_hlsysstartpackage#name"] = "eeb65a6da0fadd32bd204291efc9d808"
          , ["PK_hlsyssvccatcategory#id"] = "95efed1dd3838afd50a96fc00bc7ca2f"
          , ["PK_hlsyssvccatdocument#id"] = "49de3fda79ee0ef6c2456dd2d9993b61"
          , ["PK_hlsyssvccatfeature#id"] = "95efedbd584ab06c1836d1ed86b18013"
          , ["PK_hlsyssvccatfeaturelistitem#listitemid"] = "a708b23bdea20bd63a0d2f06fcdf5c46"
          , ["PK_hlsyssvccatorder#id"] = "599aee7dcc371fbb676e006aba5f6d16"
          , ["PK_hlsyssvccatorderitem#id"] = "b0c278b88422f1acb6ba88153adc5347"
          , ["PK_hlsyssvccatpicture#id"] = "009e1ceab03792186272dc32577d76f5"
          , ["PK_hlsyssvccatproduct#id"] = "78eeef7b42050ff79e0c29e2bca5dab4"
          , ["PK_hlsyssystemtaskhistoryplan#executiontime"] = "90254247a8cbd575e5fe6589b03f2771"
          , ["PK_hlsystablecfgdeskfield#usedefault"] = "698746caa73acdbbff63f30d03e10f76"
          , ["PK_hlsystweettoprocess#processid"] = "08d5da44bad48c720f6225d72bf7132f"
          , ["PK_hlsyswebservice#configurationname"] = "dbd2c5483a5cd7123cff01151760e651"
          , ["PK_hlsyswebservice#webserviceclient"] = "dbd2c5483a5cd7123cff01151760e651"
          , ["PK_hlsyswrkeffrtprtyassgnmnts#fromdate"] = "40463b4e5724197b1f7be2f074622be6"
          , ["PK_hltmendpointstorage#partitionkey"] = "baab535bde7cbb370ded7079f80cc102"
          , ["PK_hltmendpointstorage#rowkey"] = "baab535bde7cbb370ded7079f80cc102"
          , ["PK_hltmkeytoid#id"] = "47348df9cc513cfa4a6f8853df658212"
          , ["PK_hltmnotificationtaskstorage#partitionkey"] = "37db490ded8308b83eeaeaa84f27f8bb"
          , ["PK_hltmnotificationtaskstorage#rowkey"] = "37db490ded8308b83eeaeaa84f27f8bb"
          , ["PK_hltmservicetaskstorage#partitionkey"] = "84121c0e4b072a9de618c03d6d1223d6"
          , ["PK_hltmservicetaskstorage#rowkey"] = "84121c0e4b072a9de618c03d6d1223d6"
          , ["PK_hltmtaskchangesstorage#partitionkey"] = "4d1c046129c9c510c753c9be499c0613"
          , ["PK_hltmtaskchangesstorage#rowkey"] = "4d1c046129c9c510c753c9be499c0613"
          , ["PK_hltmtaskdatastorage#partitionkey"] = "f0d61a7b8908f7c71ad9492041dd0812"
          , ["PK_hltmtaskdatastorage#rowkey"] = "f0d61a7b8908f7c71ad9492041dd0812"
          , ["PK_hltmtasklistitem#id"] = "459eb1f52a6c57d487581ac003809c26"
          , ["PK_hltmtasklistitemextension#domain"] = "e776fce98c19229fdb23fc392db5208a"
          , ["PK_hltmtasklistitemextension#rel"] = "e776fce98c19229fdb23fc392db5208a"
          , ["PK_hltmtasksearchdata#id"] = "73b9dba094394799e716af0deee7cc85"
          , ["PK_hltpparentprocessidmapping#parentprocessid"] = "fe0f07684df0f01b034b1b272fb823b4"
          , ["PK_hltpparentprocessidmapping#processid"] = "fe0f07684df0f01b034b1b272fb823b4"
          , ["PK_hltpprocessoverview#processinstanceid"] = "961d7c1f312f6595b45eafffff57b7e1"
          , ["PK_hltpprocessoverview#taskdefinitionkey"] = "961d7c1f312f6595b45eafffff57b7e1"
          , ["PK_hlwfactionsubscription#actionname"] = "09a2bf374f6e658a5418dc13814a4683"
          , ["PK_hlwfactivityproject#assemblyversion"] = "4b790f908dedf03b3ce8e59fad12d0e4"
          , ["PK_hlwfactivityproject#projectguid"] = "4b790f908dedf03b3ce8e59fad12d0e4"
          , ["PK_hlwfcompletedscope#completedscopeid"] = "95b626a2bb63c81f15a8152ea545fe1d"
          , ["PK_hlwfinstance#instanceid"] = "dd5265102f49e937395162cc22320374"
          , ["PK_hlwfnotifconfigdefault#online"] = "e7ade2fa09ab43a872be433800f1d8aa"
          , ["PK_hlwfobjectchangedsubscription#queuename"] = "8d12cf3f464020ae0ad04cacd277eebb"
          , ["PK_hlwfprocessrolesubscription#subscriptionid"] = "a431618c14c708f448ae9ef98ac0049f"
          , ["PK_hlwfprojectchangedsubscription#queuename"] = "c40a4500c2767da549f9e5559b85a7d5"
          , ["PK_hlwfprojectchangedsubscription#workflowinstanceid"] = "c40a4500c2767da549f9e5559b85a7d5"
          , ["PK_hlwfreanalyzesubscription#queuename"] = "e0aba06ee4d5f6b81f42c5ced9eb2a6b"
          , ["PK_hlwfresprestriction#category"] = "bdadbce8cef33cd71293f6132af03d68"
        };
        private readonly IDictionary<string, TSqlFragment> _primaryKeyColumnLocations;

        public PrimaryKeyDataTypeSqlCodeAnalysisRuleVisitor()
        {
            this._primaryKeyColumnLocations = new Dictionary<string, TSqlFragment>();
        }

        protected override void BeginStatement(TSqlScript node)
        {
            PrimaryKeyColumnLocationVisitor visitor = new PrimaryKeyColumnLocationVisitor();
            node.Accept(visitor);
            this._primaryKeyColumnLocations.ReplaceWith(visitor.PrimaryKeyColumnLocations);
        }

        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            // TODO: Clarify if this rule should also be applied for UDTs
            if (tableModel is TableType)
                return;

            ICollection<Constraint> constraints = base.Model.GetConstraints(tableModel, tableName).ToArray();

            Constraint primaryKey = constraints.SingleOrDefault(x => x.Kind == ConstraintKind.PrimaryKey);
            if (primaryKey == null)
                return;

            string actualTableName = tableName.BaseIdentifier.Value;
            foreach (Column column in primaryKey.Columns)
            {
                if (column.IsComputed)
                    continue;

                // If the PK is not the table's own key, and instead is a FK to a different table's key, no further analysis is needed
                bool hasMatchingForeignKey = constraints.Where(x => x.Kind == ConstraintKind.ForeignKey)
                                                        .Any(x => x.Columns.Any(y => y.Name == column.Name));
                if (hasMatchingForeignKey)
                    continue;

                string identifier = column.Name;
                if (primaryKey.Name != null)
                    identifier = String.Concat(primaryKey.Name, '#', identifier);
                else
                    identifier = String.Concat(actualTableName, '#', identifier);

                if (PrimaryKeyDataType.AllowedTypes.Contains(column.SqlDataType))
                    continue;

                if (Suppressions.TryGetValue(identifier, out string hash) && hash == base.Hash) 
                    continue;
                
                string dataTypeName = column.SqlDataType != SqlDataType.Unknown ? column.SqlDataType.ToString() : column.DataTypeName;
                if (primaryKey.Name != null && this._primaryKeyColumnLocations.TryGetValue($"{primaryKey.Name}.{column.Name}", out TSqlFragment target))
                    base.Fail(target, actualTableName, column.Name, dataTypeName.ToUpperInvariant());
                else
                    base.Fail(column.Source, actualTableName, column.Name, dataTypeName.ToUpperInvariant());
            }
        }

        private sealed class PrimaryKeyColumnLocationVisitor : TSqlFragmentVisitor
        {
            public IDictionary<string, TSqlFragment> PrimaryKeyColumnLocations { get; }

            public PrimaryKeyColumnLocationVisitor() => this.PrimaryKeyColumnLocations = new Dictionary<string, TSqlFragment>();

            public override void Visit(TableDefinition node)
            {
                foreach (ConstraintDefinition constraint in node.TableConstraints)
                {
                    if (!(constraint is UniqueConstraintDefinition uniqueConstraint) || !uniqueConstraint.IsPrimaryKey) 
                        continue;

                    foreach (ColumnWithSortOrder column in uniqueConstraint.Columns)
                    {
                        if (constraint.ConstraintIdentifier == null) 
                            continue;

                        this.PrimaryKeyColumnLocations.Add($"{constraint.ConstraintIdentifier.Value}.{column.Column.GetName().Value}", column.Column.MultiPartIdentifier);
                    }
                }
            }
        }
    }
}