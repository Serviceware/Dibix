using System;
using System.IO;
using System.Threading.Tasks;
using Dibix.Testing.TestContainers;
using DotNet.Testcontainers.Images;
using Testcontainers.MsSql;

namespace Dibix.Dapper.Tests
{
    public sealed class ContainerServices : IAsyncDisposable
    {
        private static ContainerServices _instance;

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

            await WriteHeader(logger, serviceName).ConfigureAwait(false);

            string initializeDatabaseScript = await GetInitializeDatabaseScript().ConfigureAwait(false);
            string logFilePath = addTestRunFile($"{serviceName}.log");
            StreamWriter logWriter = File.CreateText(logFilePath);
            logWriter.AutoFlush = true;
            RedirectStdoutAndStderrToTextWriter outputConsumer = new RedirectStdoutAndStderrToTextWriter(stdout: logWriter, stderr: logWriter);

            MsSqlBuilder builder = new MsSqlBuilder().WithImage(image)
                                                     .WithOutputConsumer(outputConsumer);

            MsSqlContainer container = builder.Build();

            await builder.LogDockerRunDebugStatement(logger).ConfigureAwait(false);
            await container.StartAsync().ConfigureAwait(false);
            await container.ExecScriptAsync(initializeDatabaseScript).ConfigureAwait(false);
            await logger.WriteLineAsync("Container is ready").ConfigureAwait(false);

            MsSqlServerContainerInstance instance = new MsSqlServerContainerInstance(container, outputConsumer, container.GetConnectionString());
            return instance;
        }

        private static async Task<string> GetInitializeDatabaseScript()
        {
            const string resourceName = "InitializeDatabase.sql";
            await using Stream stream = typeof(ContainerServices).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

            using StreamReader reader = new StreamReader(stream);
            string script = await reader.ReadToEndAsync().ConfigureAwait(false);
            return script;
        }

        private static async Task WriteHeader(TextWriter logger, string message)
        {
            string border = new string('-', message.Length);
            await logger.WriteLineAsync($"""
                                         {border}
                                         {message}
                                         {border}
                                         """).ConfigureAwait(false);
        }
    }
}