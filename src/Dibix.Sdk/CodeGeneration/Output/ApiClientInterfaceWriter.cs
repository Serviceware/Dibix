using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiClientInterfaceWriter : ApiClientWriter
    {
        #region Properties
        public override string RegionName => "Interfaces";
        #endregion

        #region Overrides
        protected override void WriteController(CodeGenerationContext context, ControllerDefinition controller, string serviceName)
        {
            string interfaceName = $"I{serviceName}";
            CSharpInterface @interface = context.Output.AddInterface(interfaceName, CSharpModifiers.Public);

            foreach (ActionDefinition action in controller.Actions)
            {
                // TODO: Remove this shit!
                if (context.Model.AreaName != "Tests" && action.Target.OperationName != "GetUserConfiguration")
                    continue;

                base.AddMethod(action, context, (methodName, returnType) => @interface.AddMethod(methodName, returnType));
            }
        }
        #endregion
    }
}