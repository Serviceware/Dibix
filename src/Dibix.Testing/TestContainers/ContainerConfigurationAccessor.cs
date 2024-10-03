using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Dibix.Testing.TestContainers
{
    internal static class ContainerConfigurationAccessor
    {
        private static readonly IDictionary<Type, Func<object, IContainerConfiguration>> HandlerMap = new Dictionary<Type, Func<object, IContainerConfiguration>>();

        public static IContainerConfiguration GetConfiguration<TBuilderEntity, TContainerEntity, TConfigurationEntity>(ContainerBuilder<TBuilderEntity, TContainerEntity, TConfigurationEntity> containerBuilder) where TBuilderEntity : ContainerBuilder<TBuilderEntity, TContainerEntity, TConfigurationEntity> where TContainerEntity : IContainer where TConfigurationEntity : IContainerConfiguration
        {
            Type containerType = containerBuilder.GetType();
            if (!HandlerMap.TryGetValue(containerType, out Func<object, IContainerConfiguration> handler))
            {
                handler = TryCompileConfigurationAccessor(containerType);
                HandlerMap.Add(containerType, handler);
            }
            return handler.Invoke(containerBuilder);
        }

        private static Func<object, IContainerConfiguration> TryCompileConfigurationAccessor(Type containerType)
        {
            const string configurationPropertyName = "DockerResourceConfiguration";
            PropertyInfo configurationProperty = containerType.GetProperty(configurationPropertyName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (configurationProperty == null)
                throw new InvalidOperationException($"Could not find property 'protected {containerType}.{configurationPropertyName}'");

            ParameterExpression containerParameter = Expression.Parameter(typeof(object), "container");
            Expression instance = Expression.Convert(containerParameter, containerType);
            Expression value = Expression.Property(instance, configurationProperty);
            Expression<Func<object, IContainerConfiguration>> lambda = Expression.Lambda<Func<object, IContainerConfiguration>>(value, containerParameter);
            Func<object, IContainerConfiguration> compiled = lambda.Compile();
            return compiled;
        }
    }
}