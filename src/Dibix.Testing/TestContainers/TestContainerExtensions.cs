using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

namespace Dibix.Testing.TestContainers
{
    public static class TestContainerExtensions
    {
        public static async Task LogDockerRunDebugStatement<TBuilderEntity, TContainerEntity, TConfigurationEntity>(this ContainerBuilder<TBuilderEntity, TContainerEntity, TConfigurationEntity> containerBuilder, TextWriter logger, params string[] secrets) where TBuilderEntity : ContainerBuilder<TBuilderEntity, TContainerEntity, TConfigurationEntity> where TContainerEntity : IContainer where TConfigurationEntity : IContainerConfiguration
        {
            IContainerConfiguration configuration = GetConfiguration(containerBuilder);
            await LogDockerRunDebugStatement(configuration, logger, secrets).ConfigureAwait(false);
        }
        public static async Task LogDockerRunDebugStatement(this IContainerConfiguration configuration, TextWriter logger, params string[] secrets)
        {
            string MaskSecrets(string text) => secrets.Where(x => x != null).Aggregate(text, (current, secret) => current.Replace(secret!, "*****"));

            string containerName = configuration.Name ?? GenerateContainerName(configuration.Image);
            StringBuilder sb = new StringBuilder($"docker run --rm --tty --interactive --name {containerName}");

            IList<Mount> mounts = configuration.Mounts.Select(x => new Mount
            {
                Source = x.Source,
                Target = x.Target
            }).ToArray();
            if (configuration.ParameterModifiers != null)
            {
                CreateContainerParameters createContainerParameters = new CreateContainerParameters { HostConfig = new HostConfig { Mounts = mounts } };
                foreach (Action<CreateContainerParameters> parameterModifier in configuration.ParameterModifiers)
                    parameterModifier(createContainerParameters);

                HostConfig hostConfig = createContainerParameters.HostConfig;
                if (hostConfig.CapAdd != null && hostConfig.CapAdd.Any())
                    sb.Append($" {string.Join(" ", hostConfig.CapAdd.Select(x => $"--cap-add {x}"))}");

                if (hostConfig.SecurityOpt != null && hostConfig.SecurityOpt.Any())
                    sb.Append($" {string.Join(" ", hostConfig.SecurityOpt.Select(x => $"--security-opt {x}"))}");
            }

            if (mounts.Any())
                sb.Append($" {string.Join(" ", mounts.Select(x => $"--volume \"{x.Source}\":\"{x.Target}\"{(x.BindOptions?.Propagation != null ? $":{x.BindOptions.Propagation}" : null)}"))}");

            if (configuration.Environments.Any())
                sb.Append($" {string.Join(" ", configuration.Environments.Select(x => $"--env {x.Key}=\"{MaskSecrets(x.Value)}\""))}");

            if (configuration.PortBindings.Any())
                sb.Append($" {string.Join(" ", configuration.PortBindings.Select(x => $"--publish {x.Key}:{(!String.IsNullOrEmpty(x.Value) ? x.Value : x.Key)}"))}");

            string[] extraHosts = configuration.ExtraHosts.ToArray();
            if (extraHosts.Any())
                sb.Append($" {string.Join(" ", extraHosts.Select(x => $"--add-host {x}"))}");

            sb.Append($" {configuration.Image.FullName}");

            if (configuration.Command.Any())
                sb.Append($" {string.Join(" ", configuration.Command.Select(MaskSecrets))}");

            string command = sb.ToString();
            await logger.WriteLineAsync($"> {command}").ConfigureAwait(false);
        }

        public static IContainerConfiguration GetConfiguration<TBuilderEntity, TContainerEntity, TConfigurationEntity>(this ContainerBuilder<TBuilderEntity, TContainerEntity, TConfigurationEntity> containerBuilder) where TBuilderEntity : ContainerBuilder<TBuilderEntity, TContainerEntity, TConfigurationEntity> where TContainerEntity : IContainer where TConfigurationEntity : IContainerConfiguration => ContainerConfigurationAccessor.GetConfiguration(containerBuilder);

        public static async Task ThrowOnNonZeroExitCode(this IContainer container)
        {
            long exitCode = await container.GetExitCodeAsync().ConfigureAwait(false);
            if (exitCode == 0)
                return;

            throw await WrapException($"Container exited with exit code {exitCode}", container).ConfigureAwait(false);
        }

        public static async Task<Exception> WrapException(TimeoutException exception, IContainer container, string serviceName, int port, TimeSpan timeout)
        {
            string message = $"{serviceName} did not respond on port {port} within the given timeout: {timeout}";
            return await WrapException(message, container, exception).ConfigureAwait(false);
        }
        public static async Task<Exception> WrapException(string message, IContainer container, Exception innerException = null)
        {
            return new InvalidOperationException($"""
                                                  {message}
                                                  -
                                                  {await GetErrors(container).ConfigureAwait(false)}
                                                  -
                                                  Image: {container.Image.FullName}
                                                  Name: {(Exists(container) ? container.Name : null)}
                                                  Health: {container.Health}
                                                  State: {container.State}
                                                  """, innerException);
        }

        public static string GenerateContainerName(this IImage image)
        {
            IList<string> tokens = new Collection<string> { image.Name };
            string[] parts = image.Repository.Split(['/'], 2);
            if (parts.Length > 1)
                tokens.Insert(0, parts[1]);

            string containerName = String.Join("-", tokens);
            return containerName;
        }

        public static string GenerateImageDisplayName(this IImage image)
        {
            string imageName = image.Name.Replace("-", " ");
            string serviceName = $"{Char.ToUpperInvariant(imageName[0])}{imageName.Substring(1)}";
            return serviceName;
        }

        public static async Task WriteHeader(TextWriter logger, string message)
        {
            string border = new string('-', message.Length);
            await logger.WriteLineAsync($"""
                                         {border}
                                         {message}
                                         {border}
                                         """).ConfigureAwait(false);
        }

        private static async Task<string> GetErrors(IContainer container)
        {
            (_, string stdErr) = await container.GetLogsAsync().ConfigureAwait(false);
            string errors = stdErr.Trim();
            if (String.IsNullOrEmpty(errors))
                errors = "<No errors were written to STDERR>";

            return errors;
        }

        private static bool Exists(IContainer container)
        {
            const TestcontainersStates containerHasBeenCreatedStates = TestcontainersStates.Created | TestcontainersStates.Running | TestcontainersStates.Exited;
            return containerHasBeenCreatedStates.HasFlag(container.State);
        }
    }
}