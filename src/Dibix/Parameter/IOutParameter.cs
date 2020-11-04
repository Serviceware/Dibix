namespace Dibix
{
    public interface IOutParameter<out T>
    {
        T Result { get; }
    }
}