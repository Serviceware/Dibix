using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.Abstractions;
using Dibix.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed partial class CodeGenerationTaskTests : TestBase
    {
        private void ExecuteTest
        (
            IEnumerable<string> sources = null
          , IEnumerable<string> contracts = null
          , IEnumerable<string> endpoints = null
          , bool isEmbedded = true
          , AssertOutputKind outputKind = AssertOutputKind.Accessor
        )
        {
            string outputDirectory = Path.Combine(base.TestContext.TestRunResultsDirectory, "Output", base.TestContext.TestName);
            Directory.CreateDirectory(outputDirectory);
            const string areaName = "Tests";
            const string accessorTargetName = "TestAccessor";
            const string accessorTargetFileName = $"{accessorTargetName}.cs";
            const string endpointTargetFileName = $"{areaName}.Endpoints.cs";
            const string packageMetadataTargetFileName = $"{areaName}.PackageMetadata.json";
            const string clientTargetFileName = $"{areaName}.Client.cs";
            const string modelTargetFileName = $"{accessorTargetName}.model.json";
            const string documentationTargetName = areaName;

            TestLogger logger = new TestLogger(base.Out, distinctErrorLogging: true);

            string inputConfigurationPath = base.AddResultFile("core.input", $@"ProjectName
  {DatabaseTestUtility.ProjectName}
ProjectDirectory
  {DatabaseTestUtility.DatabaseProjectDirectory}
ConfigurationFilePath
  {Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, "dibix.json")}
LockFile
ResetLockFile
StaticCodeAnalysisSucceededFile
ResultsFile
ProductName
  Dibix.Sdk
AreaName
  {areaName}
Title
  Dibix.Sdk.Tests API title
Version
  1.0.1
Description
  Dibix.Sdk.Tests API description
OutputDirectory
  {outputDirectory}
AccessorTargetName
  {accessorTargetName}
AccessorTargetFileName
  {accessorTargetFileName}
EndpointTargetFileName
  {(outputKind == AssertOutputKind.Endpoint ? endpointTargetFileName : null)}
PackageMetadataTargetFileName
  {(outputKind == AssertOutputKind.Endpoint ? packageMetadataTargetFileName : null)}
ClientTargetFileName
  {(outputKind == AssertOutputKind.Client ? clientTargetFileName : null)}
ModelTargetFileName
  {(outputKind == AssertOutputKind.Model ? modelTargetFileName : null)}
DocumentationTargetName
  {(outputKind == AssertOutputKind.OpenApi ? documentationTargetName : null)}
ExternalAssemblyReferenceDirectory
  {Environment.CurrentDirectory}
BuildingInsideVisualStudio
Source
{CollectInputItems(sources)}
ScriptSource
Contracts
{CollectInputItems(contracts)}
Endpoints
{CollectInputItems(endpoints)}
References
DatabaseSchemaProviderName
  {DatabaseTestUtility.DatabaseSchemaProviderName}
ModelCollation
  {DatabaseTestUtility.ModelCollation}
IsEmbedded
  {isEmbedded}
LimitDdlStatements
  True
PreventDmlReferences
  True
SupportOpenApiNullableReferenceTypes
  True
SqlReferencePath");
            InputConfiguration inputConfiguration = InputConfiguration.Parse(inputConfigurationPath);
            SqlCoreTask task = new SqlCoreTask(logger, inputConfiguration);
            bool result = ((ITask)task).Execute();

            logger.Verify();

            Assert.IsTrue(result, "MSBuild task result was false");

            string accessorOutputFilePath = Path.Combine(outputDirectory, accessorTargetFileName);
            string endpointOutputFilePath = Path.Combine(outputDirectory, endpointTargetFileName);
            string clientOutputFilePath = Path.Combine(outputDirectory, clientTargetFileName);
            string modelOutputFilePath = Path.Combine(outputDirectory, modelTargetFileName);
            string openApiDocumentFilePath = Path.Combine(outputDirectory, "Tests.yml");

            switch (outputKind)
            {
                case AssertOutputKind.Accessor:
                    this.AssertFile(accessorOutputFilePath, CollectAccessorReferences());
                    Assert.IsFalse(File.Exists(endpointOutputFilePath), "File.Exists(endpointOutputFilePath)");
                    Assert.IsFalse(File.Exists(clientOutputFilePath), "File.Exists(clientOutputFilePath)");
                    Assert.IsFalse(File.Exists(modelOutputFilePath), "File.Exists(modelOutputFilePath)");
                    Assert.IsFalse(File.Exists(openApiDocumentFilePath), "File.Exists(openApiDocumentFilePath)");
                    break;

                case AssertOutputKind.Model:
                    AssertFileContent(modelOutputFilePath, actualTextNormalizer: NormalizeModelContent);
                    Assert.IsTrue(File.Exists(accessorOutputFilePath), "File.Exists(accessorOutputFilePath)");
                    Assert.IsFalse(File.Exists(endpointOutputFilePath), "File.Exists(endpointOutputFilePath)");
                    Assert.IsFalse(File.Exists(clientOutputFilePath), "File.Exists(clientOutputFilePath)");
                    Assert.IsFalse(File.Exists(openApiDocumentFilePath), "File.Exists(openApiDocumentFilePath)");
                    break;

                case AssertOutputKind.Endpoint:
                    this.AssertFile(endpointOutputFilePath, CollectEndpointReferences());
                    Assert.IsTrue(File.Exists(accessorOutputFilePath), "File.Exists(accessorOutputFilePath)");
                    Assert.IsFalse(File.Exists(clientOutputFilePath), "File.Exists(clientOutputFilePath)");
                    Assert.IsFalse(File.Exists(modelOutputFilePath), "File.Exists(modelOutputFilePath)");
                    Assert.IsFalse(File.Exists(openApiDocumentFilePath), "File.Exists(openApiDocumentFilePath)");
                    break;

                case AssertOutputKind.Client:
                    this.AssertFile(clientOutputFilePath, CollectClientReferences());
                    Assert.IsTrue(File.Exists(accessorOutputFilePath), "File.Exists(accessorOutputFilePath)");
                    Assert.IsFalse(File.Exists(endpointOutputFilePath), "File.Exists(endpointOutputFilePath)");
                    Assert.IsFalse(File.Exists(modelOutputFilePath), "File.Exists(modelOutputFilePath)");
                    Assert.IsFalse(File.Exists(openApiDocumentFilePath), "File.Exists(openApiDocumentFilePath)");
                    break;

                case AssertOutputKind.OpenApi:
                    // The OpenAPI SDK uses LF but even if we would use LF in the expected file, Git's autocrlf feature would mess it up again
                    const bool normalizeLineEndings = true;
                    this.AssertFileContent(openApiDocumentFilePath, normalizeLineEndings);
                    Assert.IsTrue(File.Exists(accessorOutputFilePath), "File.Exists(accessorOutputFilePath)");
                    Assert.IsFalse(File.Exists(endpointOutputFilePath), "File.Exists(endpointOutputFilePath)");
                    Assert.IsFalse(File.Exists(clientOutputFilePath), "File.Exists(clientOutputFilePath)");
                    Assert.IsFalse(File.Exists(modelOutputFilePath), "File.Exists(modelOutputFilePath)");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(outputKind), outputKind, null);
            }
        }

        private static string NormalizeModelContent(string text)
        {
            string jsonEscapedDatabaseProjectDirectory = $@"{DatabaseTestUtility.DatabaseProjectDirectory.Replace(@"\", @"\\")}\\";
            string regex = $"{Regex.Escape($"\"Source\": \"{jsonEscapedDatabaseProjectDirectory}")}(?<RelativeFilePath>[^\"]+)\"";
            string replaced = Regex.Replace(text, regex, x =>
            {
                string relativeFilePath = x.Groups["RelativeFilePath"].Value;
                return $"\"Source\": \"{relativeFilePath.Replace(@"\\", @"/")}\"";
            });
            return replaced;
        }

        private static string CollectInputItems(IEnumerable<string> items)
        {
            string GenerateItem(string x) => $@"  {x}
    FullPath {Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, x)}";

            return String.Join(Environment.NewLine, (items ?? Enumerable.Empty<string>()).Select(GenerateItem));
        }

        private void ExecuteTestAndExpectError
        (
            string expectedException
          , IEnumerable<string> sources = null
          , IEnumerable<string> contracts = null
          , IEnumerable<string> endpoints = null
          , bool isEmbedded = true
        )
        {
            try
            {
                this.ExecuteTest(sources, contracts, endpoints, isEmbedded);
                Assert.IsTrue(false, "CodeGenerationException was expected but not thrown");
            }
            catch (CodeGenerationException ex)
            {
                Assert.AreEqual(expectedException, ex.Message);
            }
        }

        private void AssertFile(string generatedFilePath, IEnumerable<MetadataReference> references)
        {
            this.AssertFileContent(generatedFilePath);
            AssertFileCompilation(generatedFilePath, references);
        }

        private void AssertFileContent(string generatedFilePath, bool normalizeLineEndings = false, Func<string, string> actualTextNormalizer = null) => this.AssertFileContent(base.TestContext.TestName, generatedFilePath, normalizeLineEndings, actualTextNormalizer);
        private void AssertFileContent(string expectedTextKey, string generatedFilePath, bool normalizeLineEndings = false, Func<string, string> actualTextNormalizer = null)
        {
            string actualText = Regex.Replace(File.ReadAllText(generatedFilePath)
                                            , @"This code was generated by Dibix SDK [\d]+\.[\d]+\.[\d]+\.[\d]+\."
                                            , "This code was generated by Dibix SDK 1.0.0.0.");

            if (actualTextNormalizer != null)
                actualText = actualTextNormalizer(actualText);

            string extension = Path.GetExtension(generatedFilePath).TrimStart('.');
            string resourceKey = ResourceUtility.BuildResourceKey($"CodeGeneration.{expectedTextKey}.{extension}");
            string expectedText = base.GetEmbeddedResourceContent(resourceKey);
            base.AssertEqual(expectedText, actualText, extension, normalizeLineEndings: normalizeLineEndings);
        }

        private static void AssertFileCompilation(string generatedFilePath, IEnumerable<MetadataReference> references)
        {
            using (Stream inputStream = File.OpenRead(generatedFilePath))
            {
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(inputStream), path: generatedFilePath);
                CSharpCompilation compilation = CSharpCompilation.Create(null)
                                                                 .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                                                 .AddReferences(references)
                                                                 .AddSyntaxTrees(syntaxTree);

                RoslynUtility.VerifyCompilation(compilation);
            }
        }

        private static IEnumerable<MetadataReference> CollectAccessorReferences()
        {
            yield return MetadataReferenceFactory.FromType<Object>();
            yield return MetadataReferenceFactory.FromType<System.ComponentModel.DataAnnotations.KeyAttribute>();
            yield return MetadataReferenceFactory.FromType<System.Data.CommandType>();
            yield return MetadataReferenceFactory.FromType<System.Linq.Expressions.Expression>();
            yield return MetadataReferenceFactory.FromType<System.Runtime.Serialization.DataContractAttribute>();
            yield return MetadataReferenceFactory.FromType<System.Uri>();
            yield return MetadataReference.CreateFromFile("Newtonsoft.Json.dll");
            yield return MetadataReference.CreateFromFile("Dibix.dll");
            yield return MetadataReference.CreateFromFile("Dibix.Http.Server.dll");
        }

        private static IEnumerable<MetadataReference> CollectEndpointReferences()
        {
            // This whole section is rather meh, but it's just test code ¯\_(ツ)_/¯
            const string netCoreVersion = "8.0";
            string netCorePath = LocateNetCoreReferenceAssemblyDirectory(netCoreVersion);
            string aspNetCorePath = LocateAspNetCoreReferenceAssemblyDirectory(netCoreVersion);
            string configuration = new DirectoryInfo(Environment.CurrentDirectory).Parent.Name;
            string dibixHttpServerPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "..", "src", "Dibix.Http.Server", "bin", configuration, $"net{netCoreVersion}", "Dibix.Http.Server.dll"));

            yield return MetadataReference.CreateFromFile(Path.Combine(netCorePath, "mscorlib.dll"));
            yield return MetadataReference.CreateFromFile(Path.Combine(netCorePath, "netstandard.dll"));
            yield return MetadataReference.CreateFromFile(Path.Combine(netCorePath, "System.Collections.dll"));
            yield return MetadataReference.CreateFromFile(Path.Combine(netCorePath, "System.Core.dll"));
            yield return MetadataReference.CreateFromFile(Path.Combine(netCorePath, "System.Runtime.dll"));
            yield return MetadataReference.CreateFromFile(Path.Combine(aspNetCorePath, "Microsoft.AspNetCore.Http.Abstractions.dll"));
            yield return MetadataReference.CreateFromFile(Path.Combine(aspNetCorePath, "Microsoft.AspNetCore.Mvc.Core.dll"));
            yield return MetadataReferenceFactory.FromType<System.ComponentModel.DataAnnotations.KeyAttribute>();
            yield return MetadataReferenceFactory.FromType<System.Data.CommandType>();
            yield return MetadataReferenceFactory.FromType<System.Linq.Expressions.Expression>();
            yield return MetadataReferenceFactory.FromType<System.Runtime.Serialization.DataContractAttribute>();
            yield return MetadataReferenceFactory.FromType<System.Text.Json.Serialization.JsonIncludeAttribute>();
            yield return MetadataReference.CreateFromFile("Dibix.dll");
            yield return MetadataReference.CreateFromFile(dibixHttpServerPath);
        }

        private static IEnumerable<MetadataReference> CollectClientReferences()
        {
            yield return MetadataReferenceFactory.FromType<Object>();
            yield return MetadataReferenceFactory.FromType<System.ComponentModel.DataAnnotations.KeyAttribute>();
            yield return MetadataReferenceFactory.FromType<System.Net.Http.HttpClient>();
            yield return MetadataReferenceFactory.FromType<System.Net.Http.IHttpClientFactory>();
            yield return MetadataReferenceFactory.FromType<System.Uri>();
            yield return MetadataReference.CreateFromFile("System.Net.Http.Formatting.dll");
            yield return MetadataReference.CreateFromFile("Dibix.Http.Client.dll");
        }

        private static string LocateNetCoreReferenceAssemblyDirectory(string netCoreVersion) => LocateReferenceAssemblyDirectory("Microsoft.NETCore.App.Ref", netCoreVersion);
        
        private static string LocateAspNetCoreReferenceAssemblyDirectory(string netCoreVersion) => LocateReferenceAssemblyDirectory("Microsoft.AspNetCore.App.Ref", netCoreVersion);

        private static string LocateReferenceAssemblyDirectory(string packageName, string netCoreVersion)
        {
            DirectoryInfo rootDirectory = new DirectoryInfo($@"C:\Program Files\dotnet\packs\{packageName}");
            if (!rootDirectory.Exists)
                throw new InvalidOperationException($".NET reference assembly directory not found: {rootDirectory.FullName}");

            DirectoryInfo latestVersion = rootDirectory.EnumerateDirectories($"{netCoreVersion}.*").OrderByDescending(x => x.Name).FirstOrDefault();
            if (latestVersion == null)
                throw new InvalidOperationException($"No .NET reference assembly directory found within: {rootDirectory.FullName}");

            string referenceAssemblyDirectory = Path.Combine(latestVersion.FullName, "ref", $"net{netCoreVersion}");
            if (!Directory.Exists(referenceAssemblyDirectory))
                throw new InvalidOperationException($".NET reference assembly directory not found: {referenceAssemblyDirectory}");

            return referenceAssemblyDirectory;
        }

        private static TaskItem ToTaskItem(string relativePath) => new TaskItem(relativePath) { ["FullPath"] = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, relativePath) };

        public enum AssertOutputKind
        {
            None,
            Accessor,
            Model,
            Endpoint,
            Client,
            OpenApi
        }
    }
}