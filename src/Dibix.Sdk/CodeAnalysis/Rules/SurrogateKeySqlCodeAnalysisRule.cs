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
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            ["PK_hlbitomattribute(attributeconfigid)"] = "9f147bb7f4574caf2d99c4527a7097b8"
          , ["PK_hlbitomtable(tableid)"] = "a90363cecc73851e27f977c6d6c27745"
          , ["PK_hlbpmattributedefobjectdef(attributedefobjectdefid)"] = "ca4faef246722b261bfbcec2e54cd167"
          , ["PK_hlbpmattributedefobjectdefattribute(attributedefobjectdefattributeid)"] = "13ab85ed795f9d29e945077b7255eac1"
          , ["PK_hlbpmautomatedtask(automatedtaskid)"] = "bae3b274922e492d7ee28105f43377ed"
          , ["PK_hlbpmautomatedtaskattributedef(automatedtaskattributedefid)"] = "5f49be0b432ccb8549e760d01d365bd6"
          , ["PK_hlbpmautomatedtaskname(automatedtasknameid)"] = "954a3073d16d0ca5cad1d88b98965761"
          , ["PK_hlbpmchangelogitem(changelogitemid)"] = "363dec3703b0d6cb952d8cf21c50a28c"
          , ["PK_hlbpmcontentimageheader(imageid)"] = "14e253bed37ddff03b0872e3ad29a39c"
          , ["PK_hlbpmendpoint(endpointid)"] = "88117e5fc94ef6c7a614672f87e06219"
          , ["PK_hlbpmgenericsetting(genericsettingid)"] = "41ea08b9b07d3e3ef313f350efb7ac7e"
          , ["PK_hlbpmimportfile(importfileid)"] = "3c7c05160992d181b61c2a74569e97b3"
          , ["PK_hlbpmnotificationtask(notificationtaskid)"] = "904a66985f9fcf9e26641a88c808eb46"
          , ["PK_hlbpmnotificationtaskcontent(notificationtaskcontentid)"] = "ab3e3eecf796be5d828dba9d471c8132"
          , ["PK_hlbpmnotifytaskattributedef(notificationtaskattributedefid)"] = "26304cdbedc340d7e79766965a580342"
          , ["PK_hlbpmprioritytranslation(prioritytranslationid)"] = "87be784eb1dad16ca99d649a1e82104c"
          , ["PK_hlbpmprocessattributedef(processattributedefinitionid)"] = "f5c57012af25cdf775172365b055df40"
          , ["PK_hlbpmprocessautomatedtask(processautomatedtask)"] = "b0108d679a2686616d6af43f1d80e978"
          , ["PK_hlbpmprocessdefinition(processdefinitionid)"] = "5bc4bc4923f8e1cf8a1348b320f14156"
          , ["PK_hlbpmprocessnotificationtask(processnotificationtask)"] = "509d25824fbf4f9056b17d3d0dd0ff86"
          , ["PK_hlbpmprocessshapeproperty(processshapepropertyid)"] = "1e0c3b1f0e782e1500468ec67e2dc679"
          , ["PK_hlbpmprocessusecase(processusecaseid)"] = "8a70097d4731f9ca7bdd65bf5abf9605"
          , ["PK_hlbpmprocessversion(processversionid)"] = "79f08689d21691519835b75e1a661cb4"
          , ["PK_hlbpmpropertydefinition(propertydefinitionid)"] = "7e1479c4d8bb1a45bbc4a5a4e8a30cf1"
          , ["PK_hlbpmrange(rangeid)"] = "881da11e285e44e903fe33d013daf9e7"
          , ["PK_hlbpmrule(ruleid)"] = "e3c8e71c2efd03099076762afc6db665"
          , ["PK_hlbpmruleclauserightoption(ruleclauserightoptionid)"] = "fa25653f4e833e2ef467419a1ad39fc3"
          , ["PK_hlbpmruleconditionclause(ruleconditionclauseid)"] = "66dd3b926033eb6d099f8f425b8d17ae"
          , ["PK_hlbpmruleset(rulesetid)"] = "2166196ae6a72695444e49ab01fb631d"
          , ["PK_hlbpmshape(shapeid)"] = "4d330fec99fdc3b7836045e36fc2c10b"
          , ["PK_hlbpmtranslation(translationid)"] = "807882320268452b568656e46628d2c5"
          , ["PK_hlbpmusecase(usecaseid)"] = "fefa957ccd4e546a17868ee39044b328"
          , ["PK_hlbpmusecaseattributedef(usecaseattributedefid)"] = "1f115c5b6e564cdd0911888718cab677"
          , ["PK_hlbpmusecasename(usecasenameid)"] = "62ef5b95d3635c3452041613f2dbb0a2"
          , ["PK_hlbpmusecaseparameter(usecaseparameterid)"] = "fd0740bbee79087cf89461eae4e335bd"
          , ["PK_hlbpmusecaseresponseparameters(usecaseresponseparameterid)"] = "04dc37f45d831d4817e2e568b675b8f8"
          , ["PK_hlbpmversionchangelogitem(versionchangelogitemid)"] = "bd73a858590dffd5a5085e388213d0dd"
          , ["PK_hlcmhypermedialinks(id)"] = "36f637684ddfa14f0aea4ac63b9bb2cf"
          , ["PK_hlcmstartcontent(id)"] = "65ac5c83401f8cb72fec9639991d981e"
          , ["PK_hlinfdeadletter(id)"] = "b6e983fd2cf09d665d54bdcd6cfd65f5"
          , ["PK_hlnewsentry(entryid)"] = "8fd189d50bec94e0e54d7e5b30651dfc"
          , ["PK_hlspnamedassociationusage(id)"] = "bfb34b6bb42a7d2bc5024feb9edac6fe"
          , ["PK_hlsprequiredapprovalrejection(id)"] = "ccb2b0b4e97522fc5c75affbaa8f28a8"
          , ["PK_hlsysdocument(id)"] = "3d2d42caaa70b790dae87499213fefaf"
          , ["PK_hlsysdxisetting(id)"] = "ce25d3e5e671031cd4d31e821758338f"
          , ["PK_hlsyssecinhtargetsvclogs(logid)"] = "3af62a4eafc73fb5f7e090e452cd7667"
          , ["PK_hlsysservicebrokerlog(logid)"] = "ff891c39e6535441dfe58326e4821c57"
          , ["PK_hlsysslmcntragrmchangeadhoc(id)"] = "85a95ec7dbde7020165f13ec6f8b15f0"
          , ["PK_hlsysslmcntragrmchangewf(id)"] = "ae8840aace6850ecc3880e831c9ac49e"
          , ["PK_hlsystablecfg(tablecfgid)"] = "45d2acbfeee58e0e8a0d026a4d764ecc"
          , ["PK_hlsysworkeffort(id)"] = "df7b554eec9860eb3049b7f785e238a9"
          , ["PK_hlsysworkeffortassociation(id)"] = "edb852f0ab119a6f69be22e8bee26f65"
          , ["PK_hlsysworkeffortpurpose(id)"] = "5155115cdb45105caaf26c508ef66544"
          , ["PK_hlsysworkeffortstatus(id)"] = "960ab9e3800d6b827260e796c4ffb4b0"
          , ["PK_hlsyswrkeffortpriority(id)"] = "ddf70fc6d1cd0b4d1cacd4651e25be29"
          , ["PK_hlsyswrkeffrt_location(id)"] = "20a8957edbda47dea583106249c1158b"
          , ["PK_hlsyswrkeffrtciassgnmnts(id)"] = "0526d844c73e7491d90330ad011bafbe"
          , ["PK_hlsyswrkeffrtprtyassgnmnts(id,workeffortroleid,fromdate)"] = "40463b4e5724197b1f7be2f074622be6"
          , ["PK_hlsyswrkeffrtprtyrle(id)"] = "58760f4b36184809fd3d859384763b33"
          , ["PK_hlsyswrkeffrtprtyrle(id)"] = "58760f4b36184809fd3d859384763b33"
          , ["PK_hltmtablecfgcolumn(tablecfgcolumnid)"] = "f7d4530a07d587aa9ba07e2e66dbff30"
          , ["PK_hltmtablecfgextcolumn(tablecfgcolumnid)"] = "7e8454d5180cf364dc059b4cb652d87d"
          , ["PK_hltmtaskerroritem(id)"] = "f121c1491a3806984525e47396536d30"
          , ["PK_hltmtaskreport(id)"] = "40707ef1ae1195933f625e0466d5d18d"
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
