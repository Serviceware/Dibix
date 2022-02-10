using Dibix.Sdk.Sql;

namespace Dibix.Sdk.Cli
{
    [TaskRunner("sign")]
    internal sealed class SignDacFileTaskRunner : InputConfigurationTaskRunner
    {
        public SignDacFileTaskRunner(ILogger logger) : base(logger) { }

        protected override void Execute(InputConfiguration configuration)
        {
            SignDacFileTask.Execute
            (
                dacFilePath: configuration.GetSingleValue<string>("DacFilePath")
              , isEmbedded: configuration.GetSingleValue<bool>("IsEmbedded")
              , lockRetryCount: configuration.GetSingleValue<int>("LockRetryCount")
              , logger: base.Logger
            );
        }
    }
}