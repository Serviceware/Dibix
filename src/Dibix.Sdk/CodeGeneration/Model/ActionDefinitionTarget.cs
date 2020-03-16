namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionDefinitionTarget
    {
        public string Name { get; }

        protected ActionDefinitionTarget(string name)
        {
            this.Name = name;
        }
    }
}