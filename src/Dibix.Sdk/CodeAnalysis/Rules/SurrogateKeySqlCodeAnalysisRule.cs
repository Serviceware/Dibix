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
        // helpLine suppressions
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            ["PK_hlbitomattribute(attributeconfigid)"] = "c522f76399890736a641c901a670ff9e"
          , ["PK_hlbitomtable(tableid)"] = "198a7a54f45120b39fadd661bd8150ff"
          , ["PK_hlbpmattributedefobjectdef(attributedefobjectdefid)"] = "e216a50476aec2be7c6091e76c1dff66"
          , ["PK_hlbpmattributedefobjectdefattribute(attributedefobjectdefattributeid)"] = "371eac3c8c2762accad1fedd2aef9246"
          , ["PK_hlbpmautomatedtask(automatedtaskid)"] = "ecf03f484c6a7c7f0e316b62e9ac998b"
          , ["PK_hlbpmautomatedtaskattributedef(automatedtaskattributedefid)"] = "64bd81d5725430115346d4cd2615e538"
          , ["PK_hlbpmautomatedtaskname(automatedtasknameid)"] = "c27308806265706839f71f9158916e30"
          , ["PK_hlbpmchangelogitem(changelogitemid)"] = "41ce553e1d29fb342652fe0ea673e6fb"
          , ["PK_hlbpmcontentimageheader(imageid)"] = "ab440f2cb3b9517e783c11418379d68d"
          , ["PK_hlbpmendpoint(endpointid)"] = "2fb37dbb3bf10da01264805c9d350a3a"
          , ["PK_hlbpmgenericsetting(genericsettingid)"] = "7ae0198692358133604963f6db32d4ff"
          , ["PK_hlbpmimportfile(importfileid)"] = "b9a67e82efa7b531e1e158374be6e180"
          , ["PK_hlbpmnotificationtask(notificationtaskid)"] = "f92ee2554027a7792928d1fdc1f9b08c"
          , ["PK_hlbpmnotificationtaskcontent(notificationtaskcontentid)"] = "72165eb7432941bd45e83253670f9e8d"
          , ["PK_hlbpmnotifytaskattributedef(notificationtaskattributedefid)"] = "63a03eb5ef67e3801912d9b850ae7f60"
          , ["PK_hlbpmprioritytranslation(prioritytranslationid)"] = "b554eea203010dcfd3f0f0e86aed3cfd"
          , ["PK_hlbpmprocessautomatedtask(processautomatedtask)"] = "28c7326c35664ca77803f67a48f0b897"
          , ["PK_hlbpmprocessdefinition(processdefinitionid)"] = "7fb13b4c396b2addd9d492c30fb1513d"
          , ["PK_hlbpmprocessnotificationtask(processnotificationtask)"] = "9fd90b5fd0d01fcf8a900ebf0b8d3105"
          , ["PK_hlbpmprocessshapeproperty(processshapepropertyid)"] = "ec9f1e8f48ce9e966795116010eaa53c"
          , ["PK_hlbpmprocessusecase(processusecaseid)"] = "76c7309993b9073e6d9e5451377831dc"
          , ["PK_hlbpmprocessversion(processversionid)"] = "34a4ade455f83f5ade93f87394a79d9f"
          , ["PK_hlbpmpropertydefinition(propertydefinitionid)"] = "8a3c6ae94dabd25cb2ff12851dee44d5"
          , ["PK_hlbpmrange(rangeid)"] = "d6ab3689aadde58324426596b12b8603"
          , ["PK_hlbpmrule(ruleid)"] = "6491d98d0f0d6c588a9809bf4a5ad73d"
          , ["PK_hlbpmruleclauserightoption(ruleclauserightoptionid)"] = "317b0a4baf492a0e9477959a574ca8e1"
          , ["PK_hlbpmruleconditionclause(ruleconditionclauseid)"] = "99e21c51fea010f3c10acbfb0f0e29a3"
          , ["PK_hlbpmruleset(rulesetid)"] = "de9eb2abcbc560eb44308821ed50468f"
          , ["PK_hlbpmshape(shapeid)"] = "c0a0b686136faa987b2b739020851e5a"
          , ["PK_hlbpmtranslation(translationid)"] = "dfaaf3fa0f0c590bb12e73b84b9c5e3f"
          , ["PK_hlbpmusecase(usecaseid)"] = "e717312ba1f21ec853818b95b77d308a"
          , ["PK_hlbpmusecaseattributedef(usecaseattributedefid)"] = "5b086fd9976f82e79da0c4710db23d88"
          , ["PK_hlbpmusecasename(usecasenameid)"] = "239601c888db7bad94b1ebe0157ff8bb"
          , ["PK_hlbpmusecaseparameter(usecaseparameterid)"] = "bdfdf193b754cb1c235b402c283a1ed9"
          , ["PK_hlbpmusecaseresponseparameters(usecaseresponseparameterid)"] = "9a8e883fd2e5d21924e6efdf7558abd4"
          , ["PK_hlbpmversionchangelogitem(versionchangelogitemid)"] = "504137c610889e4798e09b6766854b1b"
          , ["PK_hlcmhypermedialinks(id)"] = "bcc7fa221318f094dac7d37cfc015141"
          , ["PK_hlinfdeadletter(id)"] = "043e5582cbf497cd841cb5c8e5f7465f"
          , ["PK_hlnewsentry(entryid)"] = "cd6404bffadf54a832c4551b4cfb8834"
          , ["PK_hlspnamedassociationusage(id)"] = "4515d1f0f77d008d2c7c675f7671b7b0"
          , ["PK_hlsprequiredapprovalrejection(id)"] = "b5e273785fbb4ab7e04b44acd6d00f20"
          , ["PK_hlsysdocument(id)"] = "f9c6ba48e2c642a18205c70633f4e7ce"
          , ["PK_hlsysdxisetting(id)"] = "e27db6658ad337e24257ca5b27cfc1d0"
          , ["PK_hlsyssecinhtargetsvclogs(logid)"] = "106bd0c81c295800b91c20f9ade383ef"
          , ["PK_hlsysservicebrokerlog(logid)"] = "d0f63cad44a8f84bc63eff1695aad9fe"
          , ["PK_hlsysslmcntragrmchangeadhoc(id)"] = "6d6dba473ceb1abbf315f7c1c60cf1e2"
          , ["PK_hlsysslmcntragrmchangewf(id)"] = "46c0a061a076d85c9e4c36797a603c09"
          , ["PK_hlsystablecfg(tablecfgid)"] = "2b54c8ec71c343be7d3b992708ec5755"
          , ["PK_hlsysworkeffort(id)"] = "f2cca5e79131d701b74e0c6c9b8ec88d"
          , ["PK_hlsysworkeffortassociation(id)"] = "1b0991d3b1047cc81238f98b599f4dc9"
          , ["PK_hlsysworkeffortpurpose(id)"] = "973138ba9053083557bd6933b74d27ed"
          , ["PK_hlsysworkeffortstatus(id)"] = "2c794d3995ee63e7c812fd6e07c6653f"
          , ["PK_hlsyswrkeffortpriority(id)"] = "80e8a2a16ef4d39b25bd0f8745859320"
          , ["PK_hlsyswrkeffrt_location(id)"] = "39e4265fd68c926d30b4130a8579e187"
          , ["PK_hlsyswrkeffrtciassgnmnts(id)"] = "fe621072adb38191404c397769eebd7c"
          , ["PK_hlsyswrkeffrtprtyassgnmnts(id,workeffortroleid,fromdate)"] = "763f6fde95b0709050ce4a82108ec59e"
          , ["PK_hlsyswrkeffrtprtyrle(id)"] = "2d0b7808d3ac435d8f51fb5e5a2650d3"
          , ["PK_hltmtablecfgcolumn(tablecfgcolumnid)"] = "2f239eba4dec452b81cda400b20d5770"
          , ["PK_hltmtablecfgextcolumn(tablecfgcolumnid)"] = "f3bfd574443418f4265ff6be58ad4c96"
          , ["PK_hltmtaskerroritem(id)"] = "7ba63f1ca18eb52e1ae440aac73cb6b7"
          , ["PK_hltmtaskreport(id)"] = "5de9dc79cf8d210ffa54221c35cf2b79"
        };

        protected override string ErrorMessageTemplate => "{0}";

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

            if (Suppressions.TryGetValue(identifier, out string hash) && hash == base.Hash) 
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
