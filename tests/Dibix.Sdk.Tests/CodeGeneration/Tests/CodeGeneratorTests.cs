using System;
using System.Collections.Generic;
using System.Reflection;
using Dibix.Sdk.CodeGeneration;
using Moq;
using Xunit;
using TypeInfo = Dibix.Sdk.CodeGeneration.TypeInfo;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public sealed class CodeGeneratorTests : CodeGeneratorTestBase
    {
        [Fact]
        public void ParserTest()
        {
            IFileSystemProvider physicalFileSystemProvider = new PhysicalFileSystemProvider(ScanDirectory);
            Mock<IFileSystemProvider> fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            Mock<IErrorReporter> errorReporter = new Mock<IErrorReporter>(MockBehavior.Strict);
            Mock<ICodeGenerationContext> codeGenerationContext = new Mock<ICodeGenerationContext>(MockBehavior.Strict);
            Mock<ITypeLoader> typeLoader = new Mock<ITypeLoader>(MockBehavior.Strict);
            Mock<IAssemblyLocator> assemblyLocator = new Mock<IAssemblyLocator>(MockBehavior.Strict);
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(fileSystemProvider.Object, assemblyLocator.Object);
            typeLoaderFacade.RegisterTypeLoader(typeLoader.Object);

            fileSystemProvider.Setup(x => x.GetFiles("Dibix.Sdk.Tests.Database", It.IsAny<IEnumerable<VirtualPath>>(), It.IsAny<IEnumerable<VirtualPath>>()))
                              .Returns<string, IEnumerable<VirtualPath>, IEnumerable<VirtualPath>>(physicalFileSystemProvider.GetFiles);
            errorReporter.Setup(x => x.ReportErrors()).Returns(false);
            codeGenerationContext.SetupGet(x => x.Namespace).Returns(ProjectName);
            codeGenerationContext.SetupGet(x => x.ClassName).Returns(TestName);
            codeGenerationContext.SetupGet(x => x.TypeLoaderFacade).Returns(typeLoaderFacade);
            codeGenerationContext.SetupGet(x => x.ErrorReporter).Returns(errorReporter.Object);
            typeLoader.Setup(x => x.LoadType(It.IsAny<TypeName>(), It.IsAny<Action<string>>()))
                      .Returns((TypeName typeName, Action<string> errorHandler) =>
                      {
                          Type type = Type.GetType($"{typeName.NormalizedTypeName},{this.GetType().Assembly}", true);
                          return TypeInfo.FromClrType(type, typeName);
                      });
            string path = this.GetType().Assembly.Location;
            assemblyLocator.Setup(x => x.TryGetAssemblyLocation(ProjectName, out path)).Returns(true);

            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.Create(fileSystemProvider.Object, null, errorReporter.Object)
                .Configure(cfg => cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
                                     {
                                         x.SelectFolder("Tests/Parser")
                                          .SelectParser<SqlStoredProcedureParser>(y =>
                                          {
                                              y.Formatter<GenerateScriptSqlStatementFormatter>(); // Uses sql dom script generator
                                          });
                                     })
                                     .SelectOutputWriter<DaoWriter>(x => { x.Formatting(CommandTextFormatting.Verbatim); })
                );

            codeGenerationContext.SetupGet(x => x.Configuration).Returns(configuration);

            ICodeGenerator generator = CodeGeneratorFactory.Create(codeGenerationContext.Object);
            string generated = generator.Generate();

            Evaluate(generated);
        }

        [Fact]
        public void FluentSourcesTest()
        {
            IFileSystemProvider physicalFileSystemProvider = new PhysicalFileSystemProvider(ScanDirectory);
            Mock<IFileSystemProvider> fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            Mock<IErrorReporter> errorReporter = new Mock<IErrorReporter>(MockBehavior.Strict);
            Mock<ICodeGenerationContext> codeGenerationContext = new Mock<ICodeGenerationContext>(MockBehavior.Strict);
            Mock<ITypeLoader> typeLoader = new Mock<ITypeLoader>(MockBehavior.Strict);
            Mock<IAssemblyLocator> assemblyLocator = new Mock<IAssemblyLocator>(MockBehavior.Strict);
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(fileSystemProvider.Object, assemblyLocator.Object);
            typeLoaderFacade.RegisterTypeLoader(typeLoader.Object);

            fileSystemProvider.SetupGet(x => x.CurrentDirectory).Returns(ExecutingDirectory);
            fileSystemProvider.Setup(x => x.GetFiles("Dibix.Sdk.Tests.Database", It.IsAny<IEnumerable<VirtualPath>>(), It.IsAny<IEnumerable<VirtualPath>>()))
                              .Returns<string, IEnumerable<VirtualPath>, IEnumerable<VirtualPath>>(physicalFileSystemProvider.GetFiles);
            errorReporter.Setup(x => x.ReportErrors()).Returns(false);
            codeGenerationContext.SetupGet(x => x.TypeLoaderFacade).Returns(typeLoaderFacade);
            codeGenerationContext.SetupGet(x => x.ErrorReporter).Returns(errorReporter.Object);

            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.Create(fileSystemProvider.Object, null, errorReporter.Object)
                .Configure(cfg => cfg.AddSource("Dibix.Sdk.Tests.Database", x =>
                                     {
                                         x.SelectFolder(null, "CodeAnalysis", "Tables", "Types", "Tests/Parser", "Tests/Sources/Excluded", "Tests/Sources/dbx_tests_sources_externalsp")
                                          .SelectFile("Tests/Sources/Excluded/Nested/dbx_tests_sources_excludednested.sql");
                                     })
                                     .AddSource("Dibix.Sdk.Tests.Database", x =>
                                     {
                                         x.SelectFile("Tests/Sources/dbx_tests_sources_externalsp.sql")
                                          .SelectParser<SqlStoredProcedureParser>(y =>
                                          {
                                              y.Formatter<ExecStoredProcedureSqlStatementFormatter>();
                                          });
                                     })
                                     .AddDacPac("SSISDB.dacpac", x =>
                                     {
                                         x.SelectProcedure("[catalog].[delete_project]", "DeleteProject")
                                          .SelectParser<SqlStoredProcedureParser>(y =>
                                          {
                                              y.Formatter<ExecStoredProcedureSqlStatementFormatter>();
                                          });
                                     })
                                     .SelectOutputWriter<DaoWriter>(x =>
                                     {
                                         x.Namespace("This.Is.A.Custom.Namespace")
                                          .ClassName("Accessor")
                                          .Formatting(CommandTextFormatting.Verbatim);
                                     })
                );

            codeGenerationContext.SetupGet(x => x.Configuration).Returns(configuration);

            ICodeGenerator generator = CodeGeneratorFactory.Create(codeGenerationContext.Object);
            string generated = generator.Generate();

            Evaluate("SourcesTest", generated);
        }

        [Fact]
        public void JsonSourcesTest_SimpleSchema()
        {
            IFileSystemProvider physicalFileSystemProvider = new PhysicalFileSystemProvider(ScanDirectory);
            Mock<IFileSystemProvider> fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            Mock<IErrorReporter> errorReporter = new Mock<IErrorReporter>(MockBehavior.Strict);
            Mock<ICodeGenerationContext> codeGenerationContext = new Mock<ICodeGenerationContext>(MockBehavior.Strict);
            Mock<ITypeLoader> typeLoader = new Mock<ITypeLoader>(MockBehavior.Strict);
            Mock<IAssemblyLocator> assemblyLocator = new Mock<IAssemblyLocator>(MockBehavior.Strict);
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(fileSystemProvider.Object, assemblyLocator.Object);
            typeLoaderFacade.RegisterTypeLoader(typeLoader.Object);

            fileSystemProvider.SetupGet(x => x.CurrentDirectory).Returns(ExecutingDirectory);
            fileSystemProvider.Setup(x => x.GetFiles("Dibix.Sdk.Tests.Database", It.IsAny<IEnumerable<VirtualPath>>(), It.IsAny<IEnumerable<VirtualPath>>()))
                              .Returns<string, IEnumerable<VirtualPath>, IEnumerable<VirtualPath>>(physicalFileSystemProvider.GetFiles);
            errorReporter.Setup(x => x.ReportErrors()).Returns(false);
            codeGenerationContext.SetupGet(x => x.TypeLoaderFacade).Returns(typeLoaderFacade);
            codeGenerationContext.SetupGet(x => x.ErrorReporter).Returns(errorReporter.Object);

            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.Create(fileSystemProvider.Object, null, errorReporter.Object)
                                                                                .ParseJson(@"{
  ""dml"": {
    ""Dibix.Sdk.Tests.Database"": {
      ""include"": [
        ""./**"",
        ""Tests/Sources/Excluded/Nested/dbx_tests_sources_excludednested.sql""
      ],
      ""exclude"": [
        ""CodeAnalysis"",
        ""Tables"",
        ""Types"",
        ""Tests/Parser"",
        ""Tests/Sources/Excluded"",
        ""Tests/Sources/dbx_tests_sources_externalsp""
      ]
    }
  },
  ""ddl"": {
    ""Dibix.Sdk.Tests.Database"": {
      ""include"": ""Tests/Sources/dbx_tests_sources_externalsp.sql""
    },
    ""SSISDB.dacpac"": {
      ""include"": ""[catalog].[delete_project]""
    }
  },
  ""output"": {
    ""namespace"": ""This.Is.A.Custom.Namespace"",
    ""className"": ""Accessor"",
    ""formatting"": ""Verbatim""
  }
}");

            codeGenerationContext.SetupGet(x => x.Configuration).Returns(configuration);

            ICodeGenerator generator = CodeGeneratorFactory.Create(codeGenerationContext.Object);
            string generated = generator.Generate();

            Evaluate("SourcesTest", generated);
        }

        [Fact]
        public void JsonSourcesTest_ExtendedSchema()
        {
            IFileSystemProvider physicalFileSystemProvider = new PhysicalFileSystemProvider(ScanDirectory);
            Mock<IFileSystemProvider> fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            Mock<IErrorReporter> errorReporter = new Mock<IErrorReporter>(MockBehavior.Strict);
            Mock<ICodeGenerationContext> codeGenerationContext = new Mock<ICodeGenerationContext>(MockBehavior.Strict);
            Mock<ITypeLoader> typeLoader = new Mock<ITypeLoader>(MockBehavior.Strict);
            Mock<IAssemblyLocator> assemblyLocator = new Mock<IAssemblyLocator>(MockBehavior.Strict);
            ITypeLoaderFacade typeLoaderFacade = new TypeLoaderFacade(fileSystemProvider.Object, assemblyLocator.Object);
            typeLoaderFacade.RegisterTypeLoader(typeLoader.Object);

            fileSystemProvider.SetupGet(x => x.CurrentDirectory).Returns(ExecutingDirectory);
            fileSystemProvider.Setup(x => x.GetFiles("Dibix.Sdk.Tests.Database", It.IsAny<IEnumerable<VirtualPath>>(), It.IsAny<IEnumerable<VirtualPath>>()))
                              .Returns<string, IEnumerable<VirtualPath>, IEnumerable<VirtualPath>>(physicalFileSystemProvider.GetFiles);
            errorReporter.Setup(x => x.ReportErrors()).Returns(false);
            codeGenerationContext.SetupGet(x => x.TypeLoaderFacade).Returns(typeLoaderFacade);
            codeGenerationContext.SetupGet(x => x.ErrorReporter).Returns(errorReporter.Object);

            typeof(GeneratorConfigurationBuilder).GetField("UseExtendedJsonReader", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);

            GeneratorConfiguration configuration = GeneratorConfigurationBuilder.Create(fileSystemProvider.Object, null, errorReporter.Object)
                                                                                .ParseJson(@"{
  ""input"": {
    ""Dibix.Sdk.Tests.Database"": [
      {
        ""include"": [
          ""./**"",
          ""Tests/Sources/Excluded/Nested/dbx_tests_sources_excludednested.sql""
        ],
        ""exclude"": [
          ""CodeAnalysis"",
          ""Tables"",
          ""Types"",
          ""Tests/Parser"",
          ""Tests/Sources/Excluded"",
          ""Tests/Sources/dbx_tests_sources_externalsp""
        ]
      },
      {
        ""include"": ""Tests/Sources/dbx_tests_sources_externalsp.sql"",
        ""formatter"": ""ExecStoredProcedureSqlStatementFormatter""
      }
    ],
    ""SSISDB.dacpac"": {
      ""include"": {
        ""[catalog].[delete_project]"": ""DeleteProject""
      },
      ""formatter"": ""ExecStoredProcedureSqlStatementFormatter""
    }
  },
  ""output"": {
    ""namespace"": ""This.Is.A.Custom.Namespace"",
    ""className"": ""Accessor"",
    ""formatting"": ""Verbatim""
  }
}");

            codeGenerationContext.SetupGet(x => x.Configuration).Returns(configuration);

            ICodeGenerator generator = CodeGeneratorFactory.Create(codeGenerationContext.Object);
            string generated = generator.Generate();

            Evaluate("SourcesTest", generated);
        }
    }
}