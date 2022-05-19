using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    [TestClass]
    public sealed class SymbolNameProbingTests
    {
        private readonly ICollection<string> _schemaStore = new HashSet<string>
        {
            "Dibix.CodeAnalysis.DomainModel.Contract"
          , "Dibix.CodeGeneration.DomainModel.Runtime.StoredProcedure"
          , "Dibix.CodeGeneration.DomainModel.Runtime.Contract"
          , "Dibix.CodeGeneration.DomainModel.Contract"
        };

        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataRow(null,      "Contract",                                          "Dibix.CodeGeneration.DomainModel.Contract",         1, DisplayName = "Scenario A")]
        [DataRow(null,      "CodeAnalysis.DomainModel.Contract",                 "Dibix.CodeAnalysis.DomainModel.Contract",           2, DisplayName = "Scenario B")]
        [DataRow(null,      "DomainModel.Contract",                              "Dibix.CodeGeneration.DomainModel.Contract",         3, DisplayName = "Scenario C")]
        [DataRow(null,      "Runtime.Contract",                                  "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 1, DisplayName = "Scenario D")]
        [DataRow(null,      "Dibix.CodeGeneration.DomainModel.Runtime.Contract", "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 5, DisplayName = "Scenario E")]
        [DataRow("Runtime", "Contract",                                          "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 1, DisplayName = "Scenario F")]
        [DataRow("Runtime", "CodeAnalysis.DomainModel.Contract",                 "Dibix.CodeAnalysis.DomainModel.Contract",           2, DisplayName = "Scenario G")]
        [DataRow("Runtime", "DomainModel.Contract",                              "Dibix.CodeGeneration.DomainModel.Contract",         3, DisplayName = "Scenario H")]
        [DataRow("Runtime", "Runtime.Contract",                                  "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 4, DisplayName = "Scenario I")]
        [DataRow("Runtime", "Dibix.CodeGeneration.DomainModel.Runtime.Contract", "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 5, DisplayName = "Scenario J")]
        public void EvaluateProbingCandidates_WithSingleArea_AllScenarios(string relativeNamespace, string typeName, string expectedPath, int expectedAttempts)
        {
            EvaluateProbingCandidates_AllScenarios(areaName: "CodeGeneration", relativeNamespace, typeName, expectedPath, expectedAttempts);
        }

        [TestMethod]
        [DataRow(null,                     "CodeGeneration.Contract",                           "Dibix.CodeGeneration.DomainModel.Contract",         1, DisplayName = "Scenario A")]
        [DataRow(null,                     "CodeAnalysis.DomainModel.Contract",                 "Dibix.CodeAnalysis.DomainModel.Contract",           3, DisplayName = "Scenario B")]
        [DataRow(null,                     "CodeGeneration.DomainModel.Contract",               "Dibix.CodeGeneration.DomainModel.Contract",         3, DisplayName = "Scenario C")]
        [DataRow(null,                     "CodeGeneration.Runtime.Contract",                   "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 1, DisplayName = "Scenario D")]
        [DataRow(null,                     "Dibix.CodeGeneration.DomainModel.Runtime.Contract", "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 2, DisplayName = "Scenario E")]
        [DataRow("CodeGeneration.Runtime", "Contract",                                          "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 1, DisplayName = "Scenario F")]
        [DataRow("CodeGeneration.Runtime", "CodeAnalysis.DomainModel.Contract",                 "Dibix.CodeAnalysis.DomainModel.Contract",           3, DisplayName = "Scenario G")]
        [DataRow("CodeGeneration.Runtime", "CodeGeneration.DomainModel.Contract",               "Dibix.CodeGeneration.DomainModel.Contract",         3, DisplayName = "Scenario H")]
        [DataRow("CodeGeneration.Runtime", "CodeGeneration.Runtime.Contract",                   "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 4, DisplayName = "Scenario I")]
        [DataRow("CodeGeneration.Runtime", "Dibix.CodeGeneration.DomainModel.Runtime.Contract", "Dibix.CodeGeneration.DomainModel.Runtime.Contract", 2, DisplayName = "Scenario J")]
        public void EvaluateProbingCandidates_WithMultipleAreas_AllScenarios(string relativeNamespace, string typeName, string expectedPath, int expectedAttempts)
        {
            EvaluateProbingCandidates_AllScenarios(areaName: null, relativeNamespace, typeName, expectedPath, expectedAttempts);
        }

        private void EvaluateProbingCandidates_AllScenarios(string areaName, string relativeNamespace, string typeName, string expectedPath, int expectedAttempts)
        {
            int i = 1;
            foreach (string candidate in SymbolNameProbing.EvaluateProbingCandidates("Dibix", areaName, LayerName.DomainModel, relativeNamespace, typeName))
            {
                this.TestContext.WriteLine($"Evaluating probing candidate {i}: {candidate}");
                if (this._schemaStore.Contains(candidate))
                {
                    Assert.AreEqual(expectedPath, candidate, "Unexpected candidate match");
                    Assert.AreEqual(expectedAttempts, i, "Unexpected number of attempts");
                    return;
                }
                i++;
            }
            Assert.Fail("Probing candidates didn't match any schemas in the store");
        }
    }
}

// Sample
namespace Dibix
{
    namespace CodeAnalysis
    {
        namespace DomainModel
        {
            class Contract { }
        }
    }
    namespace CodeGeneration
    {
        namespace DomainModel
        {
            class StoredProcedure
            {
                public Contract A { get; set; }
                public CodeAnalysis.DomainModel.Contract B { get; set; }
                public DomainModel.Contract C { get; set; }
                public Runtime.Contract D { get; set; }
                public Dibix.CodeGeneration.DomainModel.Runtime.Contract E { get; set; }
            }

            namespace Runtime
            {
                class StoredProcedure
                {
                    public Contract F { get; set; }
                    public CodeAnalysis.DomainModel.Contract G { get; set; }
                    public DomainModel.Contract H { get; set; }
                    public Runtime.Contract I { get; set; }
                    public Dibix.CodeGeneration.DomainModel.Runtime.Contract K { get; set; }
                }

                class Contract { }
            }

            class Contract { }
        }
    }
}