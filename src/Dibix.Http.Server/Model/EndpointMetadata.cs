using System;
using System.Runtime.CompilerServices;

namespace Dibix.Http.Server
{
    public sealed class EndpointMetadata
    {
        private string _productName;
        private string _areaName;

        public string ProductName
        {
            // Not supported on all platforms: Dibix.Http.Host yes, Dibix.Http.Server no
            get => SafeGetValue(ref _productName);
            set => _productName = value;
        }
        public string AreaName
        {
            get => SafeGetValue(ref _areaName);
            set => _areaName = value;
        }

        private static T SafeGetValue<T>(ref T value, [CallerMemberName] string propertyName = null) => value ?? throw new InvalidOperationException($"{propertyName} is not initialized");
    }
}