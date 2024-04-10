using System.Collections.Generic;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class EndpointCodeGenerator : CodeGenerator
    {
        #region Constructor
        public EndpointCodeGenerator(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger) : base(model, schemaRegistry, logger) { }
        #endregion

        #region Overrides
        protected override IEnumerable<ArtifactWriterBase> SelectWriters(CodeGenerationModel model)
        {
            const bool assumeEmbeddedActionTargets = true;
            const CodeGenerationOutputFilter outputFilter = CodeGenerationOutputFilter.Referenced;

            // External assemblies are not supported by Dibix.Http.Host
            const bool includeReflectionTargets = false;

            // Targets with ref parameters require proxy method generation using LambdaExpression.CompileToMethod which is not supported in Dibix.Http.Host
            const bool includeTargetsWithRefParameters = false;
            
            // Deep object query parameters require a custom model binder, which is not supported in Dibix.Http.Host
            const bool includeTargetsWithDeepObjectQueryParameters = false;

            // Endpoints that use a body converter require a reflection target, which is not supported in Dibix.Http.Host
            const bool includeTargetsWithBodyConverter = false;

            // Action delegates are explicitly generated for Dibix.Http.Host and require ASP.NET Core
            const bool generateActionDelegates = true;

            yield return new DaoExecutorWriter(model, outputFilter);
            yield return new DaoExecutorInputClassWriter(model, outputFilter);
            yield return new DaoContractClassWriter(model, outputFilter, JsonSerializerFlavor.SystemTextJson);
            yield return new DaoStructuredTypeWriter(model, outputFilter);
            yield return new ApiDescriptionWriter(assumeEmbeddedActionTargets, includeReflectionTargets, includeTargetsWithRefParameters, includeTargetsWithDeepObjectQueryParameters, includeTargetsWithBodyConverter, generateActionDelegates);
        }

        protected override IEnumerable<CSharpAnnotation> CollectGlobalAnnotations(CodeGenerationModel model)
        {
            yield return new CSharpAnnotation("ArtifactAssembly");
        }

        protected override void OnContextCreated(CodeGenerationContext context)
        {
            context.AddUsing("Dibix");
        }
        #endregion
    }
}