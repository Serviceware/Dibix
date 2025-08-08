namespace Dibix
{
    [ActionParameterSource("ITEM")]
    internal sealed class ItemParameterSource : ActionParameterSourceDefinition<ItemParameterSource>
    {
        public const string IndexPropertyName = "$INDEX";
        public const string ParentPropertyName = "$PARENT";
        public const string ChildPropertyName = "$CHILD";
    }
}