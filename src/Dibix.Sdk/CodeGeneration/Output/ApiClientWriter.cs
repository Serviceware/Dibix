using System;
using System.Linq;
using System.Text;
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

            foreach (ControllerDefinition controller in context.Model.Controllers)
            {
                // TODO: Remove this shit!
                if (context.Model.AreaName != "Tests" && controller.Name != "UserConfiguration") 
                    continue;

                string serviceName = $"{controller.Name}Service";
                this.WriteController(context, controller, serviceName);
            }
        }
        #endregion

        #region Protected Methods
        protected abstract void WriteController(CodeGenerationContext context, ControllerDefinition controller, string serviceName);

        protected void AddMethod(ActionDefinition action, CodeGenerationContext context, Func<string, string, CSharpMethod> methodTarget)
        {
            context.AddUsing(typeof(Task<>).Namespace);

            string methodName = $"{action.Target.OperationName}Async";
            string returnType = ResolveReturnTypeName(action.DefaultResponseType, context);
            CSharpMethod method = methodTarget(methodName, returnType);

            foreach (ActionParameter parameter in action.Parameters.DistinctBy(x => x.ApiParameterName))
            {
                if (parameter.Location != ActionParameterLocation.Query
                 && parameter.Location != ActionParameterLocation.Path
                 && parameter.Location != ActionParameterLocation.Header)
                    continue;

                method.AddParameter(parameter.ApiParameterName, context.ResolveTypeName(parameter.Type));
            }

            context.AddUsing(typeof(CancellationToken).Namespace);
            method.AddParameter("cancellationToken", nameof(CancellationToken), default, new CSharpValue("default"));
        }
        #endregion

        #region Private Methods
        private static string ResolveReturnTypeName(TypeReference resultType, CodeGenerationContext context)
        {
            StringBuilder sb = new StringBuilder("Task<HttpResponse");

            if (resultType != null)
                sb.Append($"<{context.ResolveTypeName(resultType)}>");

            sb.Append('>');
            string typeName = sb.ToString();
            return typeName;
        }
        #endregion
    }
}