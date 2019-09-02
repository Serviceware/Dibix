namespace Dibix.Http
{
    public interface IFormattedInputBinder<in TSource, in TTarget>
    {
        void Bind(TSource source, TTarget target);
    }
}