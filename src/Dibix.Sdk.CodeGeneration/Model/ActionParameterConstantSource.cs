namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterConstantSource : ActionParameterSource
    {
        public ValueReference Value { get; }
        public override TypeReference Type => Value.Type;

        public ActionParameterConstantSource(ValueReference value) => this.Value = value;
    }
}