namespace Dibix.Http
{
    public interface IFormattedInputConverter<in TSource, out TTarget>
    {
        TTarget Convert(TSource source);
    }
}