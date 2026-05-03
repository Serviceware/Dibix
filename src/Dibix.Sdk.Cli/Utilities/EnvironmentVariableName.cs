namespace Dibix.Sdk.Cli
{
    internal static class EnvironmentVariableName
    {
        public const string HttpHostDirectory = "DIBIX_HTTP_HOST_DIRECTORY";
        public const string WorkerHostDirectory = "DIBIX_WORKER_HOST_DIRECTORY";
        public const string ConsumerDirectory = "DIBIX_CONSUMER_DIRECTORY";
        public const string NuGetPackageAlternateFeedSource = "DIBIX_NUGET_PACKAGE_ALTERNATE_FEED_SOURCE";
        public const string NuGetPackageAlternateFeedApiKey = "DIBIX_NUGET_PACKAGE_ALTERNATE_FEED_API_KEY";
        public const string NuGetPackageOfficialFeedApiKey = "DIBIX_NUGET_PACKAGE_OFFICIAL_FEED_API_KEY";
        public const string DockerHubUserName = "DIBIX_DOCKER_HUB_USER_NAME";
        public const string DockerHubPassword = "DIBIX_DOCKER_HUB_USER_PASSWORD";
    }
}