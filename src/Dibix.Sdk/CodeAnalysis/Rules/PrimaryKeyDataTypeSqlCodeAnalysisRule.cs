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
    [SqlCodeAnalysisRule(id: 23)]
    public sealed class PrimaryKeyDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        // helpLine suppressions
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            ["PK_Blob#identifier"] = "361af17b783120bf3a57eb875ad5fcfd"
          , ["PK_BlobDetail#identifier"] = "571cb352fdd9e58638c888cd8cfb6714"
          , ["PK_hlbrerulestorage#partitionkey"] = "60da11b37a21cdcfb629c867c1fef598"
          , ["PK_hlbrerulestorage#rowkey"] = "60da11b37a21cdcfb629c867c1fef598"
          , ["PK_hlcmdocumentstorage#partitionkey"] = "c6ac1b16ae67ca8a3b4aa41e66e0012f"
          , ["PK_hlcmdocumentstorage#rowkey"] = "c6ac1b16ae67ca8a3b4aa41e66e0012f"
          , ["PK_hlfrmattrlistitemref#attributepath"] = "f175bbd009ab3983914156fd3377dacc"
          , ["PK_hlfrmattrobjref#attributepath"] = "46c4ce5fae51dc1b3d5c4954f60359b1"
          , ["PK_hlfrmattrtreeitemref#attributepath"] = "6c1f8c83ada26fdf93f3ebe700922e0b"
          , ["PK_hlfrmtemplate#targetattributepath"] = "13b50b13784100b7923ccad03c64d713"
          , ["PK_hlnewsdocument#blobid"] = "05b572daecabcf80834a0f3c81128ddd"
          , ["PK_hlnewsentrylistattrref#attributekey"] = "16b66321f30663ef372061eff796cd6e"
          , ["PK_hlnewsentrystrattrref#attributekey"] = "d369bae258dfc37448d96dc7dfedc220"
          , ["PK_hlnewsentrystrattrref#value"] = "d369bae258dfc37448d96dc7dfedc220"
          , ["PK_hlpeexternalresult#processid"] = "3931dcb9296a46f8e3bfa8e7d06e85ed"
          , ["PK_hlpeexternalresult#taskid"] = "3931dcb9296a46f8e3bfa8e7d06e85ed"
          , ["PK_hlpeinstanceidtowfinstanceid#processinstanceid"] = "66079a59f554dd1066737f55946db673"
          , ["PK_hlpeoverview#instanceid"] = "b80a6c65dc8fe4c327ea83a8ef66bae5"
          , ["PK_hlpeoverviewextensions#domain"] = "c48ea8b63b33d008a17e34861384d485"
          , ["PK_hlpeoverviewextensions#instanceid"] = "c48ea8b63b33d008a17e34861384d485"
          , ["PK_hlpeoverviewextensions#rel"] = "c48ea8b63b33d008a17e34861384d485"
          , ["PK_hlpeprocessaggregatestorage#partitionkey"] = "dc3fb8bdd652fa9057c79c60f2407d3e"
          , ["PK_hlpeprocessaggregatestorage#rowkey"] = "dc3fb8bdd652fa9057c79c60f2407d3e"
          , ["PK_hlpeprocessdefinitionroutingtype#processdefinition"] = "a4811c2279a0b218d3178e3ee2744545"
          , ["PK_hlpeprocessdefinitionstorage#partitionkey"] = "4a4afdd3865029f4ad052df4569c9646"
          , ["PK_hlpeprocessdefinitionstorage#rowkey"] = "4a4afdd3865029f4ad052df4569c9646"
          , ["PK_hlpeprocessstate#processinstanceid"] = "a97879043772c901c9c7bdeec1f16709"
          , ["PK_hlpeprocessstate#taskdefinitionkey"] = "a97879043772c901c9c7bdeec1f16709"
          , ["PK_hlpetablestorage#partitionkey"] = "1f12711c1c5a08bddf0314417cffd67c"
          , ["PK_hlpetablestorage#rowkey"] = "1f12711c1c5a08bddf0314417cffd67c"
          , ["PK_hlpetaskdefpersonroutingdata#taskdefinitionkey"] = "258ac83c362b8486bea5512e4f873e8d"
          , ["PK_hlpetaskdefroleroutingdata#taskdefinitionkey"] = "163cf6214d59da0f2fa1e9a636b36399"
          , ["PK_hlpetaskdefteamroutingdata#taskdefinitionkey"] = "fce4824ff76c55d222e062fd2f914086"
          , ["PK_hlpetaskdeftoroutingtype#taskdefinitionkey"] = "0a3418ae9c2a347c454850ae9b56dc2d"
          , ["PK_hlpeversiontoprocess#processname"] = "48601c197f397f09b5376b9ada09b911"
          , ["PK_hlpeversiontoprocess#processversion"] = "48601c197f397f09b5376b9ada09b911"
          , ["PK_hlpeworkflowdefinitionstorage#partitionkey"] = "25736c3bba7096b48e4d4721b57f16c2"
          , ["PK_hlpeworkflowdefinitionstorage#rowkey"] = "25736c3bba7096b48e4d4721b57f16c2"
          , ["PK_hlpmparentprocessidmapping#parentprocessid"] = "bc32ccae85b8bcc83ff3cd34f8835b26"
          , ["PK_hlpmparentprocessidmapping#processid"] = "bc32ccae85b8bcc83ff3cd34f8835b26"
          , ["PK_hlpmprocessoverview#processinstanceid"] = "69ee820249228ac53247ce8ff5c0d83e"
          , ["PK_hlpmprocessoverview#taskdefinitionkey"] = "69ee820249228ac53247ce8ff5c0d83e"
          , ["PK_hlrptslmkpiavailability#timepoint"] = "c2e0b3e918f2cd43f68ff8714fc1af93"
          , ["PK_hlspextensionsstorage#partitionkey"] = "ef68a23249c2c1e8990418f3edd13697"
          , ["PK_hlspextensionsstorage#rowkey"] = "ef68a23249c2c1e8990418f3edd13697"
          , ["PK_hlspparentprocattrmapping#attributepath"] = "9075f64c2381f1bef23969b5438a2e81"
          , ["PK_hlspparentprocattrmapping#extensionproperty"] = "9075f64c2381f1bef23969b5438a2e81"
          , ["PK_hlspparentprocattrmapping#processdefinitionname"] = "9075f64c2381f1bef23969b5438a2e81"
          , ["PK_hlspparentprocessidmapping#parentprocessid"] = "15ba9703d9457c8b0a141a820e5dbf5d"
          , ["PK_hlspparentprocessidmapping#processid"] = "15ba9703d9457c8b0a141a820e5dbf5d"
          , ["PK_hlspprocessdefidtotaskdefkey#processdefinitionidentifier"] = "e193a30433d6e7b05e5d52893b8df652"
          , ["PK_hlspprocessdefidtotaskdefkey#taskdefinitionkey"] = "e193a30433d6e7b05e5d52893b8df652"
          , ["PK_hlspprocessdefidtowfid#processdefinitionname"] = "ed1b903db67847f7e0b5c5d3274f5bcc"
          , ["PK_hlspprocessoverview#processinstanceid"] = "d6677ab13ab828b78ebb38db37398f91"
          , ["PK_hlsptaskdefkeytolaneid#laneid"] = "14547d2487a74a96f51b45d3c30d4c07"
          , ["PK_hlsptaskdefkeytolaneid#taskdefinitionkey"] = "14547d2487a74a96f51b45d3c30d4c07"
          , ["PK_hlsptaskoverview#taskdefinitionkey"] = "74c8717b3f1409aaffc4e5da0fc85529"
          , ["PK_hlstoverview#taskid"] = "4fd781656a70fb6840b6da2158db36e4"
          , ["PK_hlstparentprocessidmapping#parentprocessid"] = "60759f556d795e57b8b2d9393d132bb4"
          , ["PK_hlstparentprocessidmapping#taskid"] = "60759f556d795e57b8b2d9393d132bb4"
          , ["PK_hlsysadhocprocessdefinition#id"] = "b10cf4d8df57bad7f7a5ce849dd6b1da"
          , ["PK_hlsysadhocprocessinstance#instanceid"] = "fc12d1e2aad99e6bf50f7469b320f308"
          , ["PK_hlsysapprovaloptions#keyname"] = "7914b83d4b75201caabdc18940c9b962"
          , ["PK_hlsysapprovements#approvementid"] = "9038f18ec5b67ed638b574c02b6670f8"
          , ["PK_hlsysbaseline#baselineid"] = "6d390ae3348f83a62c4a665c289a0cda"
          , ["PK_hlsysbaselineattr#attrid"] = "655bc34ce38a042f2a76c3cd47bdc35d"
          , ["PK_hlsysbiconfig#configname"] = "3e93d58a7abb786e8ce12a36e7cd278c"
          , ["PK_hlsysbidisplayname#resourcesetname"] = "20de98aca82c7cda47e0d899b3e71be2"
          , ["PK_hlsysbiemailconfig#configname"] = "64a4fba0f8f4c257c78572b5176152b1"
          , ["PK_hlsysbikpilastresult#kpiid"] = "2787777adcec81c52c35b358d4575d47"
          , ["PK_hlsysbimdobj#objectname"] = "a6ebf1e26b9b8758af100ad0387428f4"
          , ["PK_hlsysbimdresset#resourcetypename"] = "506f00679670c2240f6292b3857e26ea"
          , ["PK_hlsysbimdressetprop#propertyname"] = "8391a9ddb6086a8b6c75d44ce0322868"
          , ["PK_hlsysbits#binval"] = "e278c3bf3041c59f2b409aab170a4206"
          , ["PK_hlsyschangesqueue#requestid"] = "d597a9cfcf42802d2dfd71cae6916095"
          , ["PK_hlsysclientevent#clientid"] = "573c1be58047f7b4d189f752c5ee7b8e"
          , ["PK_hlsysclientevent#msgid"] = "573c1be58047f7b4d189f752c5ee7b8e"
          , ["PK_hlsyscticall#fromphone"] = "f38f9026bef678af1c4695009f08284c"
          , ["PK_hlsyscticall#tophone"] = "f38f9026bef678af1c4695009f08284c"
          , ["PK_hlsysctiuser#phone"] = "8fa2dec0357c3bca2fff4d6a541d51ac"
          , ["PK_hlsysdesignobjectdefacl#objectdefname"] = "5eb3793a3d4d00fb3a255d5bf5968314"
          , ["PK_hlsysdevicenotificationsubscription#installationid"] = "dacca48e420298047612d341fb6be144"
          , ["PK_hlsysdevicenotificationtemplate#isgrouped"] = "a35c598b6da33d29a39f0de4518d09ff"
          , ["PK_hlsyshlvfs#filepath"] = "52387091e17f23b3fe8f330fe960a24d"
          , ["PK_hlsysinqueuepending#requestid"] = "3fd5d9a9260ae587b4ddd64734d3aed0"
          , ["PK_hlsysinqueueprocessed#requestid"] = "572e518c89897fc3b50ab384c497acbd"
          , ["PK_hlsysjoblock#jobid"] = "b0a61168f4aac4d1b77fcaf0e394c516"
          , ["PK_hlsyslicexport#id"] = "7763be86ee227d819b2381b1f1a18552"
          , ["PK_hlsyslicimport#id"] = "dd0cadc1943c11f27c841c40b99a9296"
          , ["PK_hlsysliclicense#lic"] = "bae1bdd32f254caff7d42d43ea09b84c"
          , ["PK_hlsysliclicenseseat#lic"] = "b46e1750b8823c4d82e5f34994c2b3e7"
          , ["PK_hlsysliclicenseseat#token"] = "b46e1750b8823c4d82e5f34994c2b3e7"
          , ["PK_hlsyslicperseatsessions#sessionid"] = "a2218823691c7b80a5fe11af983fdeac"
          , ["PK_hlsysnotificationprocessed#processtime"] = "92a1e7e72286180ae0ecc551fdfcffae"
          , ["PK_hlsysobjectmodelassembly#assemblyname"] = "a60d3ae21b0ceb007c4609195b5587bb"
          , ["PK_hlsysoutqueuepending#requestid"] = "4c9d365dc5da2a3ddc77cdc092df3c8c"
          , ["PK_hlsysoutqueueprocessed#requestid"] = "6af37f1080a34a5df098757ae78cca6f"
          , ["PK_hlsysportalconfig#instancename"] = "db00f56842f2864e3ece85c61306ede3"
          , ["PK_hlsysportalconfigurationaudit#audittime"] = "24d91857727f4e10fb7563cabf4f1df6"
          , ["PK_hlsysprioritymatrix#matrixid"] = "726201aa35f7159accb1bd50c12e76a6"
          , ["PK_hlsysprjctprcssmp#instanceid"] = "19906e55bcc62b62971b3296b6be0b65"
          , ["PK_hlsysprocessconfig#id"] = "e04c059c10a1999e06fef25c470a4d5c"
          , ["PK_hlsysprocparambool#parametername"] = "f8f45fa23f7bc550b869cfffcbcc9a8d"
          , ["PK_hlsysprocparamdatetime#parametername"] = "2db128e392bef875c86149b8460e59f4"
          , ["PK_hlsysprocparamdecimal#parametername"] = "84eda4fcc2297d37a1502e7a60b44e63"
          , ["PK_hlsysprocparamint#parametername"] = "d47d8b6243393ca230143d9deb2cc9f6"
          , ["PK_hlsysprocparamlargetext#parametername"] = "12d009d11084c45bf44aacc9afad4273"
          , ["PK_hlsysprocparamorgunit#parametername"] = "731c536b24a8e27c9a56c414b85723ea"
          , ["PK_hlsysprocparamperson#parametername"] = "0eb4538f077015aa14ccc03ac31651ea"
          , ["PK_hlsysprocparamprocrole#parametername"] = "673130866959c298b6c6dfd0b4c60524"
          , ["PK_hlsysprocparamproduct#parametername"] = "415c34e702db633273ddac80d59ca4b8"
          , ["PK_hlsysprocparamtimespan#parametername"] = "39e5d04476c27872486e9e39343c8c70"
          , ["PK_hlsyssession#sessionid"] = "3522b04d95a4f647bd8c6666119a487a"
          , ["PK_hlsyssessionaddon#sessionidaddon"] = "e1af67048e41063c31a0e685f7540513"
          , ["PK_hlsysslmcache#cachename"] = "f464d8d61d318911c97afb34d4b63563"
          , ["PK_hlsysslmchange#changeid"] = "07728018b3bcd40fea01aafc155562d9"
          , ["PK_hlsysslmkpidefinition#id"] = "a121c9606e8856304fb4a0d20caec1ed"
          , ["PK_hlsysslmoptions#name"] = "2bdf5b4b3749b4dc59b9dd593c078153"
          , ["PK_hlsysslmprocadhoc#processid"] = "5e107615ecfd58304103748e4376923f"
          , ["PK_hlsysslmresponsibility#responsibilityid"] = "450cd21205ffeec86817df3f76bf8682"
          , ["PK_hlsysslmresponsibilitymsmq#msmqid"] = "e090061902e163643ba64fa1bb07cb56"
          , ["PK_hlsysslmservicehourshistory#changedon"] = "5db9b350a05edb0dd9e5c74937b83f66"
          , ["PK_hlsysslmslaattrmapping#attributepath"] = "3d011b836e0fb0929609b3cfb2368612"
          , ["PK_hlsysslmsvchoursentryhistory#modifiedon"] = "90630e4be3487d48d9f3baf8d740ef93"
          , ["PK_hlsysslmtablestorage#partitionkey"] = "8d2dabf4de474a93ee6a7360258bd769"
          , ["PK_hlsysslmtablestorage#rowkey"] = "8d2dabf4de474a93ee6a7360258bd769"
          , ["PK_hlsysstartpackage#name"] = "503336ed5babb6fd15ba4c63079b570e"
          , ["PK_hlsyssvccatcategory#id"] = "eb9915186b35cb3e46bc1b203a0f1f11"
          , ["PK_hlsyssvccatdocument#id"] = "529a8babb35d335e1096fa14581a99a7"
          , ["PK_hlsyssvccatfeature#id"] = "662d792e92defaa84ce90a37bd687753"
          , ["PK_hlsyssvccatfeaturelistitem#listitemid"] = "b3451b12be2d78c36095656eae7930c0"
          , ["PK_hlsyssvccatorder#id"] = "00264d3fadfc5667db2bca037d13d7ef"
          , ["PK_hlsyssvccatorderitem#id"] = "a3a8659bf4a1c36f58986d21edea5e74"
          , ["PK_hlsyssvccatpicture#id"] = "d211dead80666f41422fe5aa5ade9fd8"
          , ["PK_hlsyssvccatproduct#id"] = "eac618318800d0968f9ed5b1298ffc60"
          , ["PK_hlsyssystemtaskhistoryplan#executiontime"] = "d3d6ad1a285db63ee79f4786b4cb253b"
          , ["PK_hlsystablecfgdeskfield#usedefault"] = "ff97557cd15ffb9d0712aa6617660305"
          , ["PK_hlsystweettoprocess#processid"] = "bdb57baeb1887c51199dc538530a390e"
          , ["PK_hlsyswebservice#configurationname"] = "3399552a393d8f60d8b80ca94e5fb475"
          , ["PK_hlsyswebservice#webserviceclient"] = "3399552a393d8f60d8b80ca94e5fb475"
          , ["PK_hlsyswrkeffrtprtyassgnmnts#fromdate"] = "763f6fde95b0709050ce4a82108ec59e"
          , ["PK_hltmendpointstorage#partitionkey"] = "144b217f3945c92877c05429d915f659"
          , ["PK_hltmendpointstorage#rowkey"] = "144b217f3945c92877c05429d915f659"
          , ["PK_hltmkeytoid#id"] = "29a13b32e83969dcc63726c6d2a062b3"
          , ["PK_hltmnotificationtaskstorage#partitionkey"] = "f02e33def5d7a8f27caa761a1dd5c5d9"
          , ["PK_hltmnotificationtaskstorage#rowkey"] = "f02e33def5d7a8f27caa761a1dd5c5d9"
          , ["PK_hltmservicetaskstorage#partitionkey"] = "d1b3f85b6a4bcd23e2c70b3b7fbd8774"
          , ["PK_hltmservicetaskstorage#rowkey"] = "d1b3f85b6a4bcd23e2c70b3b7fbd8774"
          , ["PK_hltmtaskchangesstorage#partitionkey"] = "ad0d811e5c9d0fc99e829e413897b938"
          , ["PK_hltmtaskchangesstorage#rowkey"] = "ad0d811e5c9d0fc99e829e413897b938"
          , ["PK_hltmtaskdatastorage#partitionkey"] = "66de86ca644bb5256c2c1578ffde6518"
          , ["PK_hltmtaskdatastorage#rowkey"] = "66de86ca644bb5256c2c1578ffde6518"
          , ["PK_hltmtasklistitem#id"] = "71e918506f9b7f78eb633d749986ea97"
          , ["PK_hltmtasklistitemextension#domain"] = "1ce5331381b6940acb79e7bca2ed536c"
          , ["PK_hltmtasklistitemextension#rel"] = "1ce5331381b6940acb79e7bca2ed536c"
          , ["PK_hltmtasksearchdata#id"] = "02bd7da9f3f7b165940cc63ba61ba080"
          , ["PK_hltpparentprocessidmapping#parentprocessid"] = "58339d906de164e76c9b1f6c3c2396cb"
          , ["PK_hltpparentprocessidmapping#processid"] = "58339d906de164e76c9b1f6c3c2396cb"
          , ["PK_hltpprocessoverview#processinstanceid"] = "45d8baca3bee716fd404c5fe7204715b"
          , ["PK_hltpprocessoverview#taskdefinitionkey"] = "45d8baca3bee716fd404c5fe7204715b"
          , ["PK_hlwfactionsubscription#actionname"] = "f27b66b80a363ba3929421832c00e8b3"
          , ["PK_hlwfactivityproject#assemblyversion"] = "e9cba3f9a8b7166c27da278e7e9cf9ad"
          , ["PK_hlwfactivityproject#projectguid"] = "e9cba3f9a8b7166c27da278e7e9cf9ad"
          , ["PK_hlwfcompletedscope#completedscopeid"] = "c959997a9e6d822521a1b6ad65ec2ace"
          , ["PK_hlwfinstance#instanceid"] = "a2a5e74f2e35b2c5b68ab536e1a0f973"
          , ["PK_hlwfnotifconfigdefault#online"] = "2a74afafc3ff82cb911714d5a48eb4bc"
          , ["PK_hlwfobjectchangedsubscription#queuename"] = "614437d28c461a15a7a87e106647226d"
          , ["PK_hlwfprocessrolesubscription#subscriptionid"] = "06f3b44926f20d2a74b8593b5c568b7d"
          , ["PK_hlwfprojectchangedsubscription#queuename"] = "effb3e6261eb5060bd1f41073098b79b"
          , ["PK_hlwfprojectchangedsubscription#workflowinstanceid"] = "effb3e6261eb5060bd1f41073098b79b"
          , ["PK_hlwfreanalyzesubscription#queuename"] = "f57ddf72293a25d977903c36956b01f2"
          , ["PK_hlwfresprestriction#category"] = "32fd905c63b6483e31029936f6625303"
        };
        private readonly IDictionary<string, TSqlFragment> _primaryKeyColumnLocations;

        protected override string ErrorMessageTemplate => "Only TINYINT/SMALLINT/INT/BIGINT are allowed as primary key: {0}.{1} ({2})";

        public PrimaryKeyDataTypeSqlCodeAnalysisRule()
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