namespace Dibix.Sdk.Sql
{
    public enum ConstraintKind
    {
        None,
        PrimaryKey,
        ForeignKey,
        Unique,
        Check,
        Default,
        Nullable
    }
}