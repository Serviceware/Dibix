namespace Dibix
{
    internal sealed class DynamicParameterSource : ActionParameterSourceDefinition
    {
        public override string Name { get; }

        public DynamicParameterSource(string name)
        {
            this.Name = name;
        }
    }
}