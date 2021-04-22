using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Extensions.Configuration
{
    internal static class ConfigurationExtensions
    {
        public static IConfigurationBuilder AddRunSettings(this IConfigurationBuilder builder, TestContext testContext) => builder.Add(new RunSettingsConfigurationSource(testContext));

        private sealed class RunSettingsConfigurationSource : IConfigurationSource
        {
            private readonly TestContext _testContext;

            public RunSettingsConfigurationSource(TestContext testContext) => this._testContext = testContext;

            public IConfigurationProvider Build(IConfigurationBuilder builder) => new RunSettingsConfigurationProvider(this._testContext);

            private sealed class RunSettingsConfigurationProvider : ConfigurationProvider
            {
                private readonly TestContext _testContext;

                public RunSettingsConfigurationProvider(TestContext testContext) => this._testContext = testContext;

                public override void Load()
                {
                    foreach (DictionaryEntry entry in this._testContext.Properties)
                    {
                        string value = entry.Value as string;
                        if (String.IsNullOrEmpty(value))
                            continue;

                        base.Set((string)entry.Key, value);
                    }
                }
            }
        }
    }
}