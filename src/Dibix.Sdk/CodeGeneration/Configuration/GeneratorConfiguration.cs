namespace Dibix.Sdk.CodeGeneration
{
    public sealed class GeneratorConfiguration
    {
        public InputConfiguration Input { get; }
        public OutputConfiguration Output { get; }
        public bool IsInvalid { get; internal set; }

        public GeneratorConfiguration()
        {
            this.Input = new InputConfiguration();
            this.Output = new OutputConfiguration();
        }
    }
}