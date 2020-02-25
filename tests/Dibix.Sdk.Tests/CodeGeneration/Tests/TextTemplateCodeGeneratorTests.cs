using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Tests.Utilities;
using Dibix.Sdk.VisualStudio;
using EnvDTE;
using Microsoft.VisualStudio.TextTemplating;
using Moq;
using VSLangProj;
using Xunit;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed class TextTemplateCodeGeneratorTests : CodeGenerationTestBase
    {
        private static readonly string OutputDirectory = Environment.CurrentDirectory.Substring(ProjectDirectory.Length + 1);
        private static readonly string OutputFileName = Path.GetFileName(Assembly.Location);
        private static string TemplateFile => Path.GetFullPath(Path.Combine(ProjectDirectory, "CodeGeneration", String.Concat(TestName, ".tt")));

        [Fact]
        public void ParserTest()
        {
            string generated = ExecuteTest(cfg => cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
                                                     {
                                                         x.SelectFolder("Tests/Parser")
                                                          .SelectParser<SqlStoredProcedureParser>(y =>
                                                          {
                                                              y.Formatter<GenerateScriptSqlStatementFormatter>(); // Uses sql dom script generator
                                                          });
                                                     })
                                                     .SelectOutputWriter<DaoWriter>(x => { x.Formatting(CommandTextFormatting.Verbatim); }));

            Evaluate(generated);
        }

        [Fact]
        public void FluentSourcesTest()
        {
            string generated = ExecuteTest(cfg => cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
                                                     {
                                                         x.SelectFolder(null, "CodeAnalysis", "Tables", "Types", "Tests/Parser", "Tests/Sources/Excluded", "Tests/Sources/dbx_tests_sources_externalsp", "Tests/Syntax")
                                                          .SelectFile("Tests/Sources/Excluded/Nested/dbx_tests_sources_excludednested.sql");
                                                     })
                                                     .AddSource("Dibix.Sdk.Tests.Database", x =>
                                                     {
                                                         x.SelectFile("Tests/Sources/dbx_tests_sources_externalsp.sql")
                                                          .SelectParser<SqlStoredProcedureParser>(y => { y.Formatter<ExecStoredProcedureSqlStatementFormatter>(); });
                                                     })
                                                     .AddDacPac("SSISDB.dacpac", x =>
                                                     {
                                                         x.SelectProcedure("[catalog].[delete_project]", "DeleteProject")
                                                          .SelectParser<SqlStoredProcedureParser>(y => { y.Formatter<ExecStoredProcedureSqlStatementFormatter>(); });
                                                     })
                                                     .SelectOutputWriter<DaoWriter>(x =>
                                                     {
                                                         x.Namespace("This.Is.A.Custom.Namespace")
                                                          .ClassName("Accessor")
                                                          .Formatting(CommandTextFormatting.Verbatim);
                                                     }));

            Evaluate("SourcesTest", generated);
        }

        private static string ExecuteTest(Action<ICodeGeneratorConfigurationExpression> configure)
        {
            Mock<ITextTemplatingEngineHost> textTemplatingEngineHost = new Mock<ITextTemplatingEngineHost>(MockBehavior.Strict);
            Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            Mock<DTE> dte = new Mock<DTE>(MockBehavior.Strict);
            Mock<Solution> solution = new Mock<Solution>(MockBehavior.Strict);
            Mock<Projects> projects = new Mock<Projects>(MockBehavior.Strict);
            Mock<Project> project = new Mock<Project>(MockBehavior.Strict);
            Mock<Project> databaseProject = new Mock<Project>(MockBehavior.Strict);
            Mock<ConfigurationManager> configurationManager = new Mock<ConfigurationManager>(MockBehavior.Strict);
            Mock<Configuration> activeConfiguration = new Mock<Configuration>(MockBehavior.Strict);
            Mock<Properties> activeConfigurationProperties = new Mock<Properties>(MockBehavior.Strict);
            Mock<Property> outputPathProperty = new Mock<Property>(MockBehavior.Strict);
            Mock<Property> outputFileNameProperty = new Mock<Property>(MockBehavior.Strict);
            Mock<Properties> projectProperties = new Mock<Properties>(MockBehavior.Strict);
            Mock<Property> projectFullPathProperty = new Mock<Property>(MockBehavior.Strict);
            Mock<Properties> databaseProjectProperties = new Mock<Properties>(MockBehavior.Strict);
            Mock<Property> databaseProjectFullPathProperty = new Mock<Property>(MockBehavior.Strict);
            Mock<VSProject> projectObject = new Mock<VSProject>(MockBehavior.Strict);
            Mock<References> projectReferences = new Mock<References>(MockBehavior.Strict);
            Mock<ProjectItems> projectItems = new Mock<ProjectItems>(MockBehavior.Strict);
            Mock<ProjectItems> databaseProjectItems = new Mock<ProjectItems>(MockBehavior.Strict);
            Mock<ProjectItem> templateFileProjectItem = new Mock<ProjectItem>(MockBehavior.Strict);

            textTemplatingEngineHost.Setup(x => x.ResolveParameterValue("-", "-", "projectDefaultNamespace"))
                                    .Returns(ProjectName);
            textTemplatingEngineHost.SetupGet(x => x.TemplateFile).Returns(TemplateFile);
            textTemplatingEngineHost.Setup(x => x.LogErrors(It.IsAny<CompilerErrorCollection>()))
                                    .Callback((CompilerErrorCollection errors) =>
                                    {
                                        if (errors.HasErrors)
                                            throw new CodeGenerationException(errors.Cast<CompilerError>());
                                    });
            serviceProvider.Setup(x => x.GetService(typeof(DTE))).Returns(dte.Object);
            dte.SetupGet(x => x.Solution).Returns(solution.Object);
            solution.Setup(x => x.FindProjectItem(TemplateFile)).Returns(templateFileProjectItem.Object);
            solution.SetupGet(x => x.Projects).Returns(projects.Object);
            project.SetupGet(x => x.Kind).Returns((string)null);
            project.SetupGet(x => x.Name).Returns("Dibix.Sdk.Tests");
            project.SetupGet(x => x.Properties).Returns(projectProperties.Object);
            project.SetupGet(x => x.ProjectItems).Returns(projectItems.Object);
            project.SetupGet(x => x.ConfigurationManager).Returns(configurationManager.Object);
            project.SetupGet(x => x.Object).Returns(projectObject.Object);
            projectItems.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(new[]
            {
                CollectType<SpecialEntity>(),
                CollectType<Direction>()
            }.GetEnumerator);
            configurationManager.SetupGet(x => x.ActiveConfiguration).Returns(activeConfiguration.Object);
            activeConfiguration.SetupGet(x => x.Properties).Returns(activeConfigurationProperties.Object);
            activeConfigurationProperties.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(Enumerable.Repeat(outputPathProperty.Object, 1).GetEnumerator);
            outputPathProperty.SetupGet(x => x.Name).Returns("OutputPath");
            outputPathProperty.SetupGet(x => x.Value).Returns(OutputDirectory);
            outputFileNameProperty.SetupGet(x => x.Name).Returns("OutputFileName");
            outputFileNameProperty.SetupGet(x => x.Value).Returns(OutputFileName);
            projectProperties.Setup(x => x.Item("FullPath")).Returns(projectFullPathProperty.Object);
            projectProperties.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(new[]
            {
                projectFullPathProperty.Object,
                outputFileNameProperty.Object
            }.GetEnumerator);
            projectObject.SetupGet(x => x.References).Returns(projectReferences.Object);
            projectReferences.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(Enumerable.Empty<object>().GetEnumerator);
            projectFullPathProperty.SetupGet(x => x.Name).Returns("FullPath");
            projectFullPathProperty.SetupGet(x => x.Value).Returns(ProjectDirectory);
            templateFileProjectItem.SetupGet(x => x.ContainingProject).Returns(project.Object);
            databaseProject.SetupGet(x => x.Kind).Returns("{00d1a9c2-b5f0-4af3-8072-f6c62b433612}");
            databaseProject.SetupGet(x => x.Name).Returns("Dibix.Sdk.Tests.Database");
            databaseProject.SetupGet(x => x.ProjectItems).Returns(databaseProjectItems.Object);
            databaseProject.SetupGet(x => x.Properties).Returns(databaseProjectProperties.Object);
            databaseProjectProperties.Setup(x => x.Item("FullPath")).Returns(databaseProjectFullPathProperty.Object);
            databaseProjectProperties.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(new[]
            {
                databaseProjectFullPathProperty.Object,
                outputFileNameProperty.Object
            }.GetEnumerator);
            databaseProjectFullPathProperty.SetupGet(x => x.Name).Returns("FullPath");
            databaseProjectFullPathProperty.SetupGet(x => x.Value).Returns(DatabaseProjectDirectory);
            projects.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(new[]
            {
                project.Object,
                databaseProject.Object
            }.GetEnumerator);

            CollectProjectItems(DatabaseProjectDirectory, databaseProjectItems);

            string generated = TextTemplateCodeGenerator.Generate(textTemplatingEngineHost.Object, serviceProvider.Object, configure);
            return generated;
        }

        private static void CollectProjectItems(string path, Mock<ProjectItems> items)
        {
            ICollection<ProjectItem> itemsSource = new Collection<ProjectItem>();
            items.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(itemsSource.GetEnumerator);

            if (!Directory.Exists(path))
                return;

            foreach (FileSystemInfo item in new DirectoryInfo(path).EnumerateFileSystemInfos())
            {
                Mock<ProjectItem> projectItem = new Mock<ProjectItem>(MockBehavior.Strict);
                Mock<Properties> properties = new Mock<Properties>(MockBehavior.Strict);
                Mock<Property> fullPathProperty = new Mock<Property>(MockBehavior.Strict);
                Mock<ProjectItems> projectItems = new Mock<ProjectItems>(MockBehavior.Strict);

                projectItem.SetupGet(x => x.Kind).Returns(item.Attributes.HasFlag(FileAttributes.Directory) ? Constants.vsProjectItemKindPhysicalFolder : Constants.vsProjectItemKindPhysicalFile);
                projectItem.SetupGet(x => x.ProjectItems).Returns(projectItems.Object);
                projectItem.SetupGet(x => x.Properties).Returns(properties.Object);
                projectItem.SetupGet(x => x.FileCodeModel).Returns((FileCodeModel)null);
                properties.Setup(x => x.Item("FullPath")).Returns(fullPathProperty.Object);
                fullPathProperty.SetupGet(x => x.Value).Returns(item.FullName);
                items.Setup(x => x.Item(item.Name)).Returns(projectItem.Object);

                if (!item.Attributes.HasFlag(FileAttributes.Directory))
                    projectItem.SetupGet(x => x.Name).Returns(item.Name);

                itemsSource.Add(projectItem.Object);

                CollectProjectItems(item.FullName, projectItems);
            }
        }

        private static ProjectItem CollectType<TType>()
        {
            Type type = typeof(TType);

            Mock<ProjectItem> projectItem = new Mock<ProjectItem>(MockBehavior.Strict);
            Mock<FileCodeModel> codeModel = new Mock<FileCodeModel>(MockBehavior.Strict);
            Mock<ProjectItems> projectItems = new Mock<ProjectItems>(MockBehavior.Strict);
            Mock<CodeElements> codeElements = new Mock<CodeElements>(MockBehavior.Strict);
            Mock<CodeElement> codeElement = new Mock<CodeElement>(MockBehavior.Strict);

            projectItem.SetupGet(x => x.Kind).Returns((string)null);
            projectItem.SetupGet(x => x.FileCodeModel).Returns(codeModel.Object);
            projectItem.SetupGet(x => x.ProjectItems).Returns(projectItems.Object);
            codeModel.SetupGet(x => x.CodeElements).Returns(codeElements.Object);
            projectItems.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(Enumerable.Empty<object>().GetEnumerator);
            codeElement.SetupGet(x => x.FullName).Returns(type.FullName);

            if (type.IsEnum)
                codeElement.SetupGet(x => x.Kind).Returns(vsCMElement.vsCMElementEnum);
            else
            {
                codeElement.SetupGet(x => x.Kind).Returns(vsCMElement.vsCMElementClass);
                CollectConcreteType(type, codeElement);
            }

            codeElements.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(Enumerable.Repeat(codeElement.Object, 1).GetEnumerator);

            return projectItem.Object;
        }

        private static void CollectConcreteType(Type type, Mock<CodeElement> codeElement)
        {
            Mock<CodeClass> codeClass = null;
            Type currentType = type;
            while (true)
            {
                if (codeClass == null)
                    codeClass = codeElement.As<CodeClass>();

                Mock<CodeElements> bases = new Mock<CodeElements>(MockBehavior.Strict);
                Mock<CodeElements> properties = new Mock<CodeElements>(MockBehavior.Strict);

                codeClass.SetupGet(x => x.Members).Returns(properties.Object);
                properties.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(currentType.GetProperties().Select(x =>
                {
                    Mock<CodeElement> property = new Mock<CodeElement>(MockBehavior.Strict);

                    property.SetupGet(y => y.Name).Returns(x.Name);
                    property.SetupGet(y => y.Kind).Returns(vsCMElement.vsCMElementProperty);

                    return property.Object;
                }).GetEnumerator);
                codeClass.SetupGet(x => x.Bases).Returns(bases.Object);

                if (currentType.BaseType != typeof(object))
                {
                    codeClass = new Mock<CodeClass>(MockBehavior.Strict);
                    currentType = currentType.BaseType;
                    bases.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(Enumerable.Repeat(codeClass.Object, 1).GetEnumerator);
                }
                else
                {
                    bases.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(Enumerable.Empty<CodeClass>().GetEnumerator);
                    break;
                }
            }
        }
    }
}