using System;
using System.IO;
using System.Threading.Tasks;
using Dibix.Testing.TestContainers;
using DotNet.Testcontainers.Images;
using Testcontainers.MsSql;

namespace Dibix.Tests
{
    internal sealed class ContainerServices : IAsyncDisposable
    {
        private static ContainerServices? _instance;

        public MsSqlServerContainerInstance MsSqlServer { get; }
        public static bool IsInitialized => _instance != null;
        public static ContainerServices Instance
        {
            get => _instance ?? throw new InvalidOperationException("Process container services not initialized");
            private set => _instance = value;
        }

        private ContainerServices(MsSqlServerContainerInstance msSqlServer)
        {
            MsSqlServer = msSqlServer;
        }

        public static async Task CreateAsync(TextWriter logger, Func<string, string> addTestRunFile)
        {
            await logger.WriteLineAsync("Initializing container services..").ConfigureAwait(false);
            await logger.WriteLineAsync().ConfigureAwait(false);

            MsSqlServerContainerInstance msSqlServer = await CreateMsSqlServer(logger, addTestRunFile).ConfigureAwait(false);
            Instance = new ContainerServices(msSqlServer);
        }

        public async ValueTask DisposeAsync()
        {
            if (_instance != null)
            {
                await MsSqlServer.DisposeAsync().ConfigureAwait(false);
                _instance = null;
            }
        }

        private static async Task<MsSqlServerContainerInstance> CreateMsSqlServer(TextWriter logger, Func<string, string> addTestRunFile)
        {
            IImage image = new DockerImage("mcr.microsoft.com/mssql/server");
            string serviceName = image.GenerateContainerName();

            await TestContainerExtensions.WriteHeader(logger, serviceName).ConfigureAwait(false);

            string? initializeDatabaseScript = await TryGetInitializeDatabaseScript().ConfigureAwait(false);
            string logFilePath = addTestRunFile($"{serviceName}.log");
            StreamWriter logWriter = File.CreateText(logFilePath);
            logWriter.AutoFlush = true;
            RedirectStdoutAndStderrToTextWriter outputConsumer = new RedirectStdoutAndStderrToTextWriter(stdout: logWriter, stderr: logWriter);

            MsSqlContainer container = await StartMsSqlServer(image, outputConsumer, logger, logWriter).ConfigureAwait(false);
            if (initializeDatabaseScript != null)
            {
                await logger.WriteLineAsync("Initializing database").ConfigureAwait(false);
                await container.ExecScriptAsync(initializeDatabaseScript).ConfigureAwait(false);
            }
            await logger.WriteLineAsync("Container is ready").ConfigureAwait(false);

            MsSqlServerContainerInstance instance = new MsSqlServerContainerInstance(container, outputConsumer, container.GetConnectionString());
            return instance;
        }

        // The SQL Server engine sporadically aborts during container startup on the build agents, killing the whole test run:
        // DotNet.Testcontainers.Containers.ContainerNotRunningException: Container <id> exited with code 1.
        // Stderr:
        // This program has encountered a fatal error and cannot continue running at Thu Sep 17 19:42:45 2026
        // The following diagnostic information is available:
        //
        //          Reason: 0x00000002
        //    Distribution: Ubuntu 24.04.4 LTS
        //      Last errno: 11
        // Last errno text: Resource temporarily unavailable
        //
        // The crash is not reproducible and originates within the engine, therefore the container is rebuilt and started again.
        private static async Task<MsSqlContainer> StartMsSqlServer(IImage image, RedirectStdoutAndStderrToTextWriter outputConsumer, TextWriter logger, TextWriter containerLogger)
        {
            const int maxRetryCount = 1;
            for (int retry = 0; ; retry++)
            {
                MsSqlBuilder builder = new MsSqlBuilder(image).WithOutputConsumer(outputConsumer);

                MsSqlContainer container = builder.Build();

                await builder.LogDockerRunDebugStatement(logger).ConfigureAwait(false);
                try
                {
                    await container.StartAsync(failureMessage: "Container did not start in time").ConfigureAwait(false);
                    return container;
                }
                catch (Exception exception) when (retry < maxRetryCount)
                {
                    // Dispose before logging, so that no remaining container output is written after the retry header
                    await container.DisposeAsync().ConfigureAwait(false);
                    await logger.WriteLineAsync(exception.ToString()).ConfigureAwait(false);
                    await logger.WriteLineAsync($"Retrying... [{retry + 1}/{maxRetryCount}]").ConfigureAwait(false);
                    await TestContainerExtensions.WriteHeader(containerLogger, $"Container failed to start. Retrying.. [{retry + 1}/{maxRetryCount}]").ConfigureAwait(false);
                }
            }
        }

        private static async Task<string?> TryGetInitializeDatabaseScript()
        {
            const string resourceName = "InitializeDatabase.sql";
            await using Stream? stream = typeof(ContainerServices).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;
                //throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

            using StreamReader reader = new StreamReader(stream);
            string script = await reader.ReadToEndAsync().ConfigureAwait(false);
            return script;
        }
    }
}