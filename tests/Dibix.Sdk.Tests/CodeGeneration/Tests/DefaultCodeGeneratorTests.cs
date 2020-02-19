//using Dibix.Sdk.CodeGeneration;
//using Moq;
//using Xunit;

//namespace Dibix.Sdk.Tests.CodeGeneration
//{
//    public sealed class DefaultCodeGeneratorTests
//    {
//        [Fact]
//        public void ParserTest()
//        {
//            Mock<IFileSystemProvider> fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
//            Mock<IErrorReporter> errorReporter = new Mock<IErrorReporter>(MockBehavior.Strict);

//            ICodeGenerationContext codeGenerationContext = new DefaultCodeGenerationContext
//            (
//                areaName: "Tests"
//              , rootNamespace: "Dibix.Sdk.Tests"
//              , defaultOutputName: null
//              , statements: new[] { new  }
//              , contracts: context.Contracts
//              , endpoints: context.Controllers
//              , userDefinedTypes: context.UserDefinedTypes
//              , codeArtifactKind: this.CodeArtifactKind
//              , contractResolver: context.ContractResolver
//              , errorReporter: context.ErrorReporter
//            );

//            ICodeGenerator generator = CodeGeneratorFactory.Create(codeGenerationContext);
//            string generated = generator.Generate();
//            return generated;
//        }
//    }
//}