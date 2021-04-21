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
            context.AddDibixHttpClientReference();
            context.AddUsing(typeof(Uri).Namespace);

            IDictionary<ActionDefinition, string> operationIdMap = context.Model
                                                                          .Controllers
                                                                          .SelectMany(x => x.Actions)
                                                                          .GroupBy(x => x.Target.OperationName)
                                                                          .SelectMany(x => x.Select((y, i) => new
                                                                          {
                                                                              Position = i + 1,
                                                                              Name = x.Key,
                                                                              Action = y,
                                                                              IsAmbiguous = x.Count() > 1
                                                                          }))
                                                                          .ToDictionary(x => x.Action, x => x.IsAmbiguous ? $"{x.Name}{x.Position}" : x.Name);

            for (int i = 0; i < context.Model.Controllers.Count; i++)
            {
                ControllerDefinition controller = context.Model.Controllers[i];
                string serviceName = $"{controller.Name}Service";
                this.WriteController(context, controller, serviceName, operationIdMap);

                if (i + 1 < context.Model.Controllers.Count)
                    context.Output.AddSeparator();
            }
        }
        #endregion

        #region Protected Methods
        protected abstract void WriteController(CodeGenerationContext context, ControllerDefinition controller, string serviceName, IDictionary<ActionDefinition, string> operationIdMap);

        protected void AddMethod(ActionDefinition action, CodeGenerationContext context, IDictionary<ActionDefinition, string> operationIdMap, Func<string, string, CSharpMethod> methodTarget)
        {
            context.AddUsing(typeof(Task<>).Namespace);

            string methodName = $"{operationIdMap[action]}Async";
            string returnType = ResolveReturnTypeName(action.DefaultResponseType, context);
            CSharpMethod method = methodTarget(methodName, returnType);

            foreach (ActionParameter parameter in action.Parameters.DistinctBy(x => x.ApiParameterName))
            {
                if (parameter.Location != ActionParameterLocation.Query
                 && parameter.Location != ActionParameterLocation.Path
                 && parameter.Location != ActionParameterLocation.Header)
                    continue;

                method.AddParameter(parameter.ApiParameterName, ResolveParameterTypeName(parameter, context));
            }

            if (action.RequestBody != null)
                method.AddParameter("body", context.ResolveTypeName(action.RequestBody.Contract));

            context.AddUsing(typeof(CancellationToken).Namespace);
            method.AddParameter("cancellationToken", nameof(CancellationToken), default, new CSharpValue("default"));
        }

        protected bool IsStream(TypeReference typeReference) => typeReference is PrimitiveTypeReference primitiveTypeReference && primitiveTypeReference.Type == PrimitiveType.Stream;
        #endregion

        #region Private Methods
        private static string ResolveReturnTypeName(TypeReference resultType, CodeGenerationContext context)
        {
            string typeName = $"Task<HttpResponse{(resultType != null ? $"<{context.ResolveTypeName(resultType)}>" : "Message")}>";
            return typeName;
        }

        private static string ResolveParameterTypeName(ActionParameter parameter, CodeGenerationContext context)
        {
            if (parameter.Location == ActionParameterLocation.Header)
                return "string";

            if (parameter.Type is SchemaTypeReference schemaTypeReference && context.GetSchema(schemaTypeReference) is UserDefinedTypeSchema userDefinedTypeSchema)
            {
                // Note: Deep object query parameters require a separate input class, which is not yet supported
                // Therefore in this case we currently return object, which obviously will not work at runtime
                string enumerableTypeName = userDefinedTypeSchema.Properties.Count == 1 ? context.ResolveTypeName(userDefinedTypeSchema.Properties[0].Type) : "object";
                context.AddUsing(typeof(IEnumerable<>).Namespace);
                return $"{nameof(IEnumerable<object>)}<{enumerableTypeName}>";
            }

            return context.ResolveTypeName(parameter.Type);
        }
        #endregion
    }
}