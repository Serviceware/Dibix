namespace Dibix
{
    internal sealed class ItemParameterSource : ActionParameterSourceDefinition<ItemParameterSource>
    {
        public const string IndexPropertyName = "$INDEX";
        public override string Name => "ITEM";
    }
}