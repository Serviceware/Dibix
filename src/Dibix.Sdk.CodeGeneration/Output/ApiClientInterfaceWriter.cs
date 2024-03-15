using System.Collections.Generic;
using System.Linq;
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
            CSharpInterface @interface = output.AddInterface(interfaceName, CSharpModifiers.Public)
                                               .Implements("IHttpService");

            foreach (ActionDefinition action in controller.Actions.OrderBy(x => operationIdMap[x]))
            {
                AddMethod(action, context, operationIdMap, @interface.AddMethod);
            }
        }
        #endregion
    }
}