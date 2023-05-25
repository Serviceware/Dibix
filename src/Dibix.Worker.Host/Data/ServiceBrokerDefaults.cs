namespace Dibix.Worker.Host
{
    internal static class ServiceBrokerDefaults
    {
        public const int CommandTimeout = 60;                        // seconds
        public const int ReceiveTimeout = CommandTimeout / 2 * 1000; // ms
    }
}