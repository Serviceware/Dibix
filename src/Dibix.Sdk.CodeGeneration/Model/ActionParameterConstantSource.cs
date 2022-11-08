namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterConstantSource : ActionParameterSource
    {
        public ValueReference Value { get; }

        public ActionParameterConstantSource(ValueReference value) => this.Value = value;
    }
}