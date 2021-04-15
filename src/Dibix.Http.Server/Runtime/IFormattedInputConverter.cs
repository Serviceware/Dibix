namespace Dibix.Http.Server
{
    public interface IFormattedInputConverter<in TSource, out TTarget>
    {
        TTarget Convert(TSource source);
    }
}