using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public static class TestConfigurationLoader
    {

        public static T Load<T>(TestContext testContext) where T : LazyConfiguration, new()
        {
            T instance = new T();
            new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true)
                                      .AddJsonFile($"appsettings.{System.Environment.MachineName}.json", optional: true)
                                      .AddRunSettings(testContext)
                                      .Build()
                                      .Bind(instance, x => x.BindNonPublicProperties = true);
            return instance;
        }
    }
}