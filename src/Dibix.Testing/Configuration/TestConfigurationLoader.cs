using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal static class TestConfigurationLoader
    {
        public static T Load<T>(TestContext testContext) where T : LazyConfiguration, new()
        {
            T instance = new T();
            IConfigurationRoot root = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true)
                                                                .AddJsonFile($"appsettings.{System.Environment.MachineName}.json", optional: true)
                                                                .AddRunSettings(testContext)
                                                                .Build();

            CollectConfigurationSections(root, testContext).Each(x => x.Bind(instance, y => y.BindNonPublicProperties = true));

            return instance;
        }

        private static IEnumerable<IConfiguration> CollectConfigurationSections(IConfiguration root, TestContext testContext)
        {
            yield return root;

            if (!TryGetProfileName(testContext, out string profileName))
                profileName = "Default";

            yield return root.GetSection($"Profiles:{profileName}");
        }

        private static bool TryGetProfileName(TestContext testContext, out string profileName)
        {
            MethodInfo testMethod = TestImplementationResolver.ResolveTestMethod(testContext);
            if (TryGetProfileName(testMethod, out profileName))
                return true;

            Type testClass = testMethod.DeclaringType;
            if (TryGetProfileName(testClass, out profileName))
                return true;

            profileName = null;
            return false;
        }

        private static bool TryGetProfileName(MemberInfo memberInfo, out string profileName)
        {
            ConfigurationProfileAttribute attribute = memberInfo.GetCustomAttribute<ConfigurationProfileAttribute>();
            if (attribute != null)
            {
                profileName = attribute.ProfileName;
                return true;
            }

            profileName = null;
            return false;
        }
    }
}