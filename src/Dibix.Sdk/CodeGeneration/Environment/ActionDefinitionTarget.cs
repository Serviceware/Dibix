namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinitionTarget
    {
        public bool IsExternal { get; }
        public string Target { get; }

        public ActionDefinitionTarget(bool isExternal, string target)
        {
            this.IsExternal = isExternal;
            this.Target = target;
        }
    }
}