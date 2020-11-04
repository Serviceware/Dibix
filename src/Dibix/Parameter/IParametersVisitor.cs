namespace Dibix
{
    public interface IParametersVisitor
    {
        void VisitInputParameters(InputParameterVisitor visitParameter);
        void VisitOutputParameters(OutputParameterVisitor visitParameter);
    }
}