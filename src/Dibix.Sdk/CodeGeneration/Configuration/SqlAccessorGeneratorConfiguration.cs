namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlAccessorGeneratorConfiguration
    {
        public InputConfiguration Input { get; }
        public OutputConfiguration Output { get; }

        public SqlAccessorGeneratorConfiguration()
        {
            this.Input = new InputConfiguration();
            this.Output = new OutputConfiguration();
        }
    }
}