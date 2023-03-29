using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal static class TestConfigurationLoader
    {
        public static T Load<T>(TestContext testContext, Action<T> initializationAction = null) where T : new()
        {
            IConfigurationRoot root = new ConfigurationBuilder().AddUserSecrets("dibix")
                                                                .AddEnvironmentVariables()
                                                                .AddJsonFile("appsettings.json", optional: true)
                                                                .AddJsonFile($"appsettings.{System.Environment.MachineName}.json", optional: true)
                                                                .AddRunSettings(testContext)
                                                                .Build();

            ConfigurationInitializationToken initializationToken = new ConfigurationInitializationToken();
            T instance = ConfigurationProxyBuilder.BuildProxyIfNeeded<T>(initializationToken);
            CollectConfigurationSections(root, testContext).Each(x => Microsoft.Extensions.Configuration.Dibix.ConfigurationBinder.Bind(x, instance));
            initializationAction?.Invoke(instance);
            initializationToken.IsInitialized = true;

            return instance;
        }

        private static IEnumerable<IConfiguration> CollectConfigurationSections(IConfiguration root, TestContext testContext)
        {
            yield return root;

            if (!TryGetProfileName(root, testContext, out string profileName))
                profileName = "Default";

            IConfigurationSection profile = root.GetSection($"Profiles:{profileName}");
            if (profile.Exists())
                yield return profile;
        }

        private static bool TryGetProfileName(IConfiguration configuration, TestContext testContext, out string profileName)
        {
            profileName = configuration["Profile"];
            if (profileName != null)
                return true;

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