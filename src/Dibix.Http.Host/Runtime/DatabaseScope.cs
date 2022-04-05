using System;

namespace Dibix.Http.Host
{
    internal sealed class DatabaseScope
    {
        private string? _initiatorFullName;

        public string InitiatorFullName
        {
            get
            {
                if (_initiatorFullName == null)
                    throw new InvalidOperationException($"{nameof(InitiatorFullName)} property not initialized");

                return _initiatorFullName;
            }
            set => _initiatorFullName = value;
        }
    }
}
