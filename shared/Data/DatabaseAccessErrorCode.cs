namespace Dibix
{
#if DIBIX_RUNTIME
    public
#else
    internal
#endif
    enum DatabaseAccessErrorCode
    {
        None,
        SequenceContainsNoElements,
        SequenceContainsMoreThanOneElement
    }
}