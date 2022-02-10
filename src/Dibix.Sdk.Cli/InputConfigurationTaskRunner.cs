namespace Dibix.Sdk.Cli
{
    internal abstract class InputConfigurationTaskRunner : TaskRunner
    {
        protected InputConfigurationTaskRunner(ILogger logger) : base(logger) { }

        protected sealed override bool Execute(string[] args)
        {
            if (args.Length < 2)
                return false;

            string inputConfigurationFile = args[1];
            InputConfiguration configuration = InputConfiguration.Parse(inputConfigurationFile);
            base.BuildingInsideVisualStudio = configuration.GetSingleValue<bool>("BuildingInsideVisualStudio", throwOnInvalidKey: false);
            this.Execute(configuration);
            return true;
        }

        protected abstract void Execute(InputConfiguration configuration);
    }
}