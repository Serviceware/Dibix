namespace Dibix.Sdk.Sql
{
    internal enum ConstraintType
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