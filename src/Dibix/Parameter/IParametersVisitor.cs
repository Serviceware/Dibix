namespace Dibix
{
    public interface IParametersVisitor
    {
        void VisitParameters(ParameterVisitor visitParameter);
    }
}