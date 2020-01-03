namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinitionTarget
    {
        public bool IsExternal { get; }
        public string Target { get; }
        public string TypeName { get; }
        public string MethodName { get; }

        private ActionDefinitionTarget(bool isExternal, string target, string typeName, string methodName)
        {
            this.IsExternal = isExternal;
            this.Target = target;
            this.TypeName = typeName;
            this.MethodName = methodName;
        }

        public static ActionDefinitionTarget Local(string target, string typeName, string methodName) => new ActionDefinitionTarget(false, target, typeName, methodName);
        public static ActionDefinitionTarget External(string target) => new ActionDefinitionTarget(true, target, null, null);
    }
}