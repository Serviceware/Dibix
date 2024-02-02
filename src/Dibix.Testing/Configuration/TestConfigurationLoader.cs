using System;
using System.Collections.Generic;
using System.Reflection;
using Dibix.Testing.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public static class TestConfigurationLoader
    {
        public static T Load<T>(TestContext testContext, TestConfigurationValidationBehavior validationBehavior = TestDefaults.ValidationBehavior, Action<T> initializationAction = null) where T : class, new()
        {
            IConfigurationRoot root = new ConfigurationBuilder().AddUserSecrets(TestImplementationResolver.ResolveTestAssembly(testContext))
                                                                .AddEnvironmentVariables()
                                                                .AddJsonFile("appsettings.json", optional: true)
                                                                .AddJsonFile($"appsettings.{Environment.MachineName}.json", optional: true)
                                                                .AddRunSettings(testContext)
                                                                .Build();

            T instance = BindConfigurationInstance(root, testContext, validationBehavior, initializationAction);

            return instance;
        }

        private static T BindConfigurationInstance<T>(IConfiguration configuration, TestContext testContext, TestConfigurationValidationBehavior validationBehavior, Action<T> initializationAction) where T : class, new()
        {
            switch (validationBehavior)
            {
                case TestConfigurationValidationBehavior.None: return BindConfigurationInstanceWithNoValidation<T>(configuration);
                case TestConfigurationValidationBehavior.Lazy: return BindConfigurationInstanceWithLazyValidation(configuration, testContext, initializationAction);
                case TestConfigurationValidationBehavior.DataAnnotations: return BindConfigurationInstanceWithDataAnnotationsValidation<T>(configuration);
                default: throw new ArgumentOutOfRangeException(nameof(validationBehavior), validationBehavior, null);
            }
        }

        private static T BindConfigurationInstanceWithNoValidation<T>(IConfiguration configuration) where T : new()
        {
            T instance = new T();
            configuration.Bind(instance);
            return instance;
        }

        private static T BindConfigurationInstanceWithLazyValidation<T>(IConfiguration configuration, TestContext testContext, Action<T> initializationAction) where T : new()
        {
            ConfigurationInitializationToken initializationToken = new ConfigurationInitializationToken();
            T instance = ConfigurationProxyBuilder.BuildProxyIfNeeded<T>(initializationToken);
            CollectConfigurationSections(configuration, testContext).Each(x => Microsoft.Extensions.Configuration.Dibix.ConfigurationBinder.Bind(x, instance));
            initializationAction?.Invoke(instance);
            initializationToken.IsInitialized = true;
            return instance;
        }

        private static T BindConfigurationInstanceWithDataAnnotationsValidation<T>(IConfiguration configuration) where T : class
        {
            ServiceCollection services = new ServiceCollection();
            services.AddOptions<T>()
                    .Bind(configuration)
                    .ValidateDataAnnotations();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IOptions<T> testOptions = serviceProvider.GetRequiredService<IOptions<T>>();
            return testOptions.Value;
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