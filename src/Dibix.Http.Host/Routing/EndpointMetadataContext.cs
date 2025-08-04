using System;

namespace Dibix.Http.Host
{
    internal sealed class EndpointMetadataContext
    {
        private string? _actionName;
        private string[]? _validAudiences;

        public string ActionName => SafeGetProperty(ref _actionName);
        public string[] ValidAudiences => SafeGetProperty(ref _validAudiences);
        public bool IsInitialized { get; private set; }

        public void Initialize(string actionName, string[] validAudiences)
        {
            _actionName = actionName;
            _validAudiences = validAudiences;
            IsInitialized = true;
        }

        private static T SafeGetProperty<T>(ref T? result)
        {
            if (Equals(result, null))
                throw new InvalidOperationException("Endpoint metadata not initialized or not available");

            return result;
        }
    }
}