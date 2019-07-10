namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterConstantSource : ActionParameterSource
    {
        public object Value { get; }

        internal ActionParameterConstantSource(object value) => this.Value = value;
    }
}