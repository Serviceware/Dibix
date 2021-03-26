namespace Dibix
{
    public abstract class ParametersVisitor
    {
        public static ParametersVisitor Empty { get; } = new EmptyParameters();

        public abstract void VisitInputParameters(InputParameterVisitor visitParameter);
        public abstract void VisitOutputParameters(OutputParameterVisitor visitParameter);

        private sealed class EmptyParameters : ParametersVisitor
        {
            public override void VisitInputParameters(InputParameterVisitor visitParameter) { }

            public override void VisitOutputParameters(OutputParameterVisitor visitParameter) { }
        }
    }
}