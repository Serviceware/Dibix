namespace Microsoft.Extensions.Configuration
{
    internal static class BindConfigurationExtensions
    {
        public static TOptions Bind<TOptions>(this IConfiguration configuration) where TOptions : new()
        {
            TOptions instance = new TOptions();
            configuration.Bind(instance);
            return instance;
        }
    }
}