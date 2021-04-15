namespace Dibix.Http.Server
{
    public interface IFormattedInputBinder<in TSource, in TTarget>
    {
        void Bind(TSource source, TTarget target);
    }
}