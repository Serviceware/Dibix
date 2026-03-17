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
            IConfigurationRoot root = LoadConfiguration(testContext);
            T instance = BindConfigurationInstance(root, testContext, validationBehavior, initializationAction);
            return instance;
        }

        private static IConfigurationRoot LoadConfiguration(TestContext testContext)
        {
            IConfigurationRoot root = new ConfigurationBuilder().AddUserSecrets(TestImplementationResolver.ResolveTestAssembly(testContext))
                                                                .AddEnvironmentVariables()
                                                                .AddJsonFile("appsettings.json", optional: true)
                                                                .AddJsonFile($"appsettings.{Environment.MachineName}.json", optional: true)
                                                                .AddRunSettings(testContext)
                                                                .Build();
            return root;
        }

        private static T BindConfigurationInstance<T>(IConfiguration configuration, TestContext testContext, TestConfigurationValidationBehavior validationBehavior, Action<T> initializationAction) where T : class, new() => validationBehavior switch
        {
            TestConfigurationValidationBehavior.None => BindConfigurationInstanceWithNoValidation<T>(configuration),
            TestConfigurationValidationBehavior.LazyUsingProxy => BindConfigurationInstanceWithLazyValidationUsingProxy(configuration, testContext, initializationAction),
            TestConfigurationValidationBehavior.LazyUsingSourceGeneration => BindConfigurationInstanceWithLazyValidationUsingSourceGeneration(configuration, testContext, initializationAction),
            TestConfigurationValidationBehavior.DataAnnotations => BindConfigurationInstanceWithDataAnnotationsValidation<T>(configuration),
            _ => throw new ArgumentOutOfRangeException(nameof(validationBehavior), validationBehavior, null)
        };

        private static T BindConfigurationInstanceWithNoValidation<T>(IConfiguration configuration) where T : new()
        {
            T instance = new T();
            configuration.Bind(instance);
            return instance;
        }

        private static T BindConfigurationInstanceWithLazyValidationUsingSourceGeneration<T>(IConfiguration configuration, TestContext testContext, Action<T> initializationAction) where T : class, new()
        {
            static T CreateConfigurationInstance(ConfigurationInitializationToken initializationToken)
            {
                T instance = new T();
                if (instance is ConfigurationInitializationTracker configurationInitializationTracker)
                    configurationInitializationTracker.PropertyInitializationTracker = new ConfigurationPropertyInitializationTracker(initializationToken);

                return instance;
            }

            return BindConfigurationInstanceWithLazyValidation(configuration, testContext, CreateConfigurationInstance, initializationAction);
        }

        private static T BindConfigurationInstanceWithLazyValidationUsingProxy<T>(IConfiguration configuration, TestContext testContext, Action<T> initializationAction) where T : new()
        {
            return BindConfigurationInstanceWithLazyValidation(configuration, testContext, ConfigurationProxyBuilder.BuildProxyIfNeeded<T>, initializationAction);
        }

        private static T BindConfigurationInstanceWithLazyValidation<T>(IConfiguration configuration, TestContext testContext, Func<ConfigurationInitializationToken, T> factory, Action<T> initializationAction)
        {
            ConfigurationInitializationToken initializationToken = new ConfigurationInitializationToken();
            T instance = factory(initializationToken);
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
            if (testMethod == null)
                return false;

            if (TryGetProfileName(testMethod, out profileName))
                return true;

            Type testClass = testMethod.DeclaringType;
            if (TryGetProfileName(testClass, out profileName))
                return true;

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