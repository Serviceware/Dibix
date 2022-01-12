using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiClientInterfaceWriter : ApiClientWriter
    {
        #region Properties
        public override string RegionName => "Interfaces";
        #endregion

        #region Overrides
        protected override void WriteController(CodeGenerationContext context, CSharpStatementScope output, ControllerDefinition controller, string serviceName, IDictionary<ActionDefinition, string> operationIdMap, IDictionary<string, SecurityScheme> securitySchemeMap)
        {
            string interfaceName = $"I{serviceName}";
            CSharpInterface @interface = output.AddInterface(interfaceName, CSharpModifiers.Public);

            foreach (ActionDefinition action in controller.Actions)
            {
                base.AddMethod(action, context, operationIdMap, (methodName, returnType) => @interface.AddMethod(methodName, returnType));
            }
        }
        #endregion
    }
}