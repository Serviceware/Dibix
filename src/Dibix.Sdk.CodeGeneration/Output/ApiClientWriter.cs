using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            IDictionary<ActionDefinition, string> operationIdMap = controllers.SelectMany(x => x.Actions)
                                                                              .GroupBy(x => x.OperationId)
                                                                              .SelectMany(x => x.Select((y, i) => new
                                                                              {
                                                                                  Position = i + 1,
                                                                                  Name = x.Key,
                                                                                  Action = y,
                                                                                  IsAmbiguous = x.Count() > 1
                                                                              }))
                                                                              .ToDictionary(x => x.Action, x => x.IsAmbiguous ? $"{x.Name}{x.Position}" : x.Name);

            IDictionary<string, SecurityScheme> securitySchemeMap = context.Model.SecuritySchemes.ToDictionary(x => x.Name);

            CSharpStatementScope output = context.CreateOutputScope();
            for (int i = 0; i < controllers.Count; i++)
            {
                ControllerDefinition controller = controllers[i];
                string serviceName = $"{controller.Name}Service";
                WriteController(context, output, controller, serviceName, operationIdMap, securitySchemeMap);

                if (i + 1 < controllers.Count)
                    output.AddSeparator();
            }
        }
        #endregion

        #region Protected Methods
        protected abstract void WriteController(CodeGenerationContext context, CSharpStatementScope output, ControllerDefinition controller, string serviceName, IDictionary<ActionDefinition, string> operationIdMap, IDictionary<string, SecurityScheme> securitySchemeMap);

        protected static void AddMethod(ActionDefinition action, CodeGenerationContext context, IDictionary<ActionDefinition, string> operationIdMap, Func<string, string, CSharpMethod> methodTarget)
        {
            context.AddUsing<Task<object>>();

            string methodName = $"{operationIdMap[action]}Async";
            string returnType = ResolveReturnTypeName(action.DefaultResponseType, context);
            CSharpMethod method = methodTarget(methodName, returnType);

            foreach (ActionParameter parameter in action.Parameters.DistinctBy(x => x.ApiParameterName))
            {
                if (parameter.ParameterLocation != ActionParameterLocation.Path)
                    continue;

                AppendParameter(context, parameter, method);
            }

            if (action.RequestBody != null)
                method.AddParameter("body", context.ResolveTypeName(action.RequestBody.Contract));

            foreach (ActionParameter parameter in action.Parameters.DistinctBy(x => x.ApiParameterName).OrderBy(x => x.DefaultValue != null))
            {
                if (parameter.ParameterLocation != ActionParameterLocation.Query
                 && parameter.ParameterLocation != ActionParameterLocation.Header)
                    continue;

                // We don't support out parameters in REST APIs, but this accessor could still be used directly within the backend
                // Therefore we discard this parameter
                if (parameter.IsOutput)
                    continue;

                // Will be handled by SecurityScheme/IHttpAuthorizationProvider
                if (parameter.ParameterLocation == ActionParameterLocation.Header && (parameter.ApiParameterName == "Authorization" || action.SecuritySchemes.Requirements.Any(x => x.Scheme.Name == parameter.ApiParameterName)))
                    continue;

                AppendParameter(context, parameter, method);
            }

            context.AddUsing<CancellationToken>();
            method.AddParameter("cancellationToken", nameof(CancellationToken), new CSharpValue("default"), default);
        }

        protected bool IsStream(TypeReference typeReference) => typeReference is PrimitiveTypeReference primitiveTypeReference && primitiveTypeReference.Type == PrimitiveType.Stream;
        #endregion

        #region Private Methods
        private static string ResolveReturnTypeName(TypeReference resultType, CodeGenerationContext context)
        {
            string typeName = $"Task<HttpResponse{(resultType != null ? $"<{context.ResolveTypeName(resultType, EnumerableBehavior.Collection)}>" : "Message")}>";
            return typeName;
        }

        private static void AppendParameter(CodeGenerationContext context, ActionParameter parameter, CSharpMethod method)
        {
            string normalizedApiParameterName = context.NormalizeApiParameterName(parameter.ApiParameterName);
            CSharpValue defaultValue = parameter.DefaultValue != null ? context.BuildDefaultValueLiteral(parameter.DefaultValue) : null;
            method.AddParameter(normalizedApiParameterName, ResolveParameterTypeName(parameter, context), defaultValue);
        }

        private static string ResolveParameterTypeName(ActionParameter parameter, CodeGenerationContext context)
        {
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