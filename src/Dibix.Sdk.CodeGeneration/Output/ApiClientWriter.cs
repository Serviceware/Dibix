using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ApiClientWriter : ArtifactWriterBase
    {
        #region Properties
        public override string LayerName => CodeGeneration.LayerName.Client;
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Controllers.Any();

        public override void Write(CodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Client");
            context.AddUsing<Uri>();

            IList<ControllerDefinition> controllers = context.Model
                                                             .Controllers
                                                             .Where(x => x.Actions.Any())
                                                             .OrderBy(x => x.Name)
                                                             .ToArray();

            IDictionary<string, SecurityScheme> securitySchemeMap = context.Model.SecuritySchemes.ToDictionary(x => x.SchemeName);

            CSharpStatementScope output = context.CreateOutputScope();
            for (int i = 0; i < controllers.Count; i++)
            {
                ControllerDefinition controller = controllers[i];
                string serviceName = $"{controller.Name}Service";
                WriteController(context, output, controller, serviceName, securitySchemeMap);

                if (i + 1 < controllers.Count)
                    output.AddSeparator();
            }
        }
        #endregion

        #region Protected Methods
        protected abstract void WriteController(CodeGenerationContext context, CSharpStatementScope output, ControllerDefinition controller, string serviceName, IDictionary<string, SecurityScheme> securitySchemeMap);

        protected static void AddMethod(ActionDefinition action, CodeGenerationContext context, Func<string, string, CSharpMethod> methodTarget)
        {
            context.AddUsing<Task<object>>();

            string methodName = $"{action.OperationId.Value}Async";
            string returnType = ResolveReturnTypeName(action.DefaultResponseType, context);
            CSharpMethod method = methodTarget(methodName, returnType);

            // Path parameters first, then body, then the rest (query, header and special body parameters)
            foreach (ApiParameter parameter in action.ApiParameters
                                                     .OrderBy(x => x.ParameterLocation != ActionParameterLocation.Path)
                                                     .ThenBy(x => x.ParameterLocation != ActionParameterLocation.Body)
                                                     .ThenBy(x => x.DefaultValue != null))
            {
                // We don't support out parameters in REST APIs, but this accessor could still be used directly within the backend
                // Therefore we discard this parameter
                if (parameter.IsOutput)
                    continue;

                switch (parameter.ParameterLocation)
                {
                    case ActionParameterLocation.NonUser:

                    // No request body contract members
                    case ActionParameterLocation.Body when parameter.ParameterName is not (SpecialHttpParameterName.Body
                                                                                        or SpecialHttpParameterName.MediaType
                                                                                        or SpecialHttpParameterName.FileName
                                                                                        or SpecialHttpParameterName.Length):

                    // Will be handled by SecurityScheme/IHttpAuthorizationProvider
                    case ActionParameterLocation.Header when parameter.ParameterName == "Authorization" || action.SecuritySchemes.Requirements.Any(x => x.Scheme.SchemeName == parameter.ParameterName):
                        continue;

                    default:
                        AppendParameter(context, parameter, action, method);
                        break;
                }
            }

            context.AddUsing<CancellationToken>();
            method.AddParameter("cancellationToken", nameof(CancellationToken), new CSharpValue("default"), default);
        }
        #endregion

        #region Private Methods
        private static string ResolveReturnTypeName(TypeReference resultType, CodeGenerationContext context)
        {
            string typeName = $"Task<HttpResponse{(resultType != null ? $"<{context.ResolveTypeName(resultType, EnumerableBehavior.Collection)}>" : "Message")}>";
            return typeName;
        }

        private static void AppendParameter(CodeGenerationContext context, ApiParameter parameter, ActionDefinition action, CSharpMethod method)
        {
            string normalizedApiParameterName = context.NormalizeApiParameterName(parameter.ParameterName);
            CSharpValue defaultValue = parameter.DefaultValue != null ? context.BuildDefaultValueLiteral(parameter.DefaultValue) : null;
            method.AddParameter(normalizedApiParameterName, ResolveParameterTypeName(parameter, action, context), defaultValue);
        }

        private static string ResolveParameterTypeName(ApiParameter parameter, ActionDefinition action, CodeGenerationContext context)
        {
            if (parameter.ParameterName == SpecialHttpParameterName.Body && action.RequestBody?.TreatAsFile != null)
            {
                return context.ResolveTypeName(PrimitiveType.Stream, parameter.Type);
            }

            if (parameter.Type.IsUserDefinedType(context.SchemaRegistry, out UserDefinedTypeSchema userDefinedTypeSchema))
            {
                // Note: Deep object query parameters require a separate input class, which is not yet supported
                // Therefore in this case we currently return object, which obviously will not work at runtime
                string enumerableTypeName = userDefinedTypeSchema.Properties.Count == 1 ? context.ResolveTypeName(userDefinedTypeSchema.Properties[0].Type, enumerableBehavior: EnumerableBehavior.None) : "object";
                return context.WrapInEnumerable(enumerableTypeName);
            }
            return context.ResolveTypeName(parameter.Type);
        }
        #endregion
    }
}