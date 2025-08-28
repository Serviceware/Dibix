using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Dibix.Http;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class PackageMetadataUnit : CodeArtifactGenerationUnit
    {
        public override bool ShouldGenerate(CodeGenerationModel model) => !String.IsNullOrEmpty(model.PackageMetadataTargetFileName);

        public override bool Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            string packageMetadataPath = Path.GetFullPath(Path.Combine(model.OutputDirectory, model.PackageMetadataTargetFileName));
            ArtifactPackageMetadata metadata = CollectMetadata(model);
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                Converters = { new StringEnumConverter() }
            };
            using Stream stream = File.Open(packageMetadataPath, FileMode.Create);
            using TextWriter textWriter = new StreamWriter(stream);
            serializer.Serialize(textWriter, metadata);
            return true;
        }

        private static ArtifactPackageMetadata CollectMetadata(CodeGenerationModel model)
        {
            IEnumerable<HttpControllerDefinitionMetadata> controllers = CollectControllers(model);
            ArtifactPackageMetadata metadata = new ArtifactPackageMetadata(controllers.ToArray());
            return metadata;
        }

        private static IEnumerable<HttpControllerDefinitionMetadata> CollectControllers(CodeGenerationModel model)
        {
            foreach (ControllerDefinition controllerDefinition in model.Controllers)
            {
                HttpControllerDefinitionMetadata controllerMetadata = new HttpControllerDefinitionMetadata(controllerDefinition.Name, CollectActions(model, controllerDefinition).ToArray());
                yield return controllerMetadata;
            }
        }

        private static IEnumerable<HttpActionDefinitionMetadata> CollectActions(CodeGenerationModel model, ControllerDefinition controllerDefinition)
        {
            foreach (ActionDefinition actionDefinition in controllerDefinition.Actions)
            {
                HttpActionDefinitionMetadata actionMetadata = new HttpActionDefinitionMetadata
                (
                    actionName: actionDefinition.OperationId,
                    relativeNamespace: actionDefinition.Target.RelativeNamespace,
                    uri: new Uri(RouteBuilder.BuildRoute(model.AreaName, controllerDefinition.Name, actionDefinition.ChildRoute), UriKind.Relative),
                    method: actionDefinition.Method,
                    childRoute: actionDefinition.ChildRoute?.Value,
                    fileResponse: actionDefinition.FileResponse != null ? new HttpFileResponseDefinition(actionDefinition.FileResponse.Cache) : null,
                    description: actionDefinition.Description,
                    modelContextProtocolType: actionDefinition.ModelContextProtocolType,
                    securitySchemes: actionDefinition.SecuritySchemes.Requirements.Select(x => x.Scheme.SchemeName).ToArray(),
                    requiredClaims: CollectRequiredClaims(actionDefinition).ToArray(),
                    statusCodeDetectionResponses: CollectStatusCodeResponses(actionDefinition).GroupBy(x => x.StatusCode).ToDictionary(x => x.Key, x => x.Last()),
                    validAudiences: []
                );
                yield return actionMetadata;
            }
        }

        private static IEnumerable<string> CollectRequiredClaims(ActionDefinition actionDefinition)
        {
            ActionTargetDefinition[] targets = [actionDefinition, ..actionDefinition.Authorization.Select(x => x)];
            foreach (ActionParameter parameter in targets.SelectMany(x => x.Parameters))
            {
                if (parameter.ParameterSource is ActionParameterClaimSource claimSource)
                    yield return claimSource.ClaimType;
            }
        }

        private static IEnumerable<HttpErrorResponse> CollectStatusCodeResponses(ActionDefinition actionDefinition)
        {
            foreach (KeyValuePair<DatabaseAccessErrorCode, HttpErrorResponse> response in HttpStatusCodeDetection.DatabaseErrorCodeHttpStatusMap)
            {
                if (!CollectDetectedStatusCodeResponse(actionDefinition, response.Key))
                    continue;

                HttpErrorResponse errorResponse = response.Value;
                if (!actionDefinition.DisabledAutoDetectionStatusCodes.Contains(errorResponse.StatusCode))
                {
                    yield return errorResponse;
                }
            }

            foreach (KeyValuePair<HttpStatusCode, ActionResponse> response in actionDefinition.Responses)
            {
                int httpStatusCode = (int)response.Key;
                ActionResponse actionResponse = response.Value;
                ErrorDescription error = actionResponse.StatusCodeDetectionDetail;

                if (error == null)
                    continue;

                int errorCode = error.ErrorCode;
                string errorMessage = error.Description;
                yield return new HttpErrorResponse(httpStatusCode, errorCode, errorMessage);
            }
        }

        private static bool CollectDetectedStatusCodeResponse(ActionDefinition action, DatabaseAccessErrorCode databaseErrorCode)
        {
            switch (databaseErrorCode)
            {
                case DatabaseAccessErrorCode.SequenceContainsNoElements:
                {
                    if (action.Target is LocalActionTarget localActionTarget)
                    {
                        bool hasSingleResult = localActionTarget.SqlStatementDefinition.Results.Any(x => x.ResultMode == SqlQueryResultMode.Single);
                        return hasSingleResult;
                    }
                    return false;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(databaseErrorCode), databaseErrorCode, null);
            }
        }
    }
}