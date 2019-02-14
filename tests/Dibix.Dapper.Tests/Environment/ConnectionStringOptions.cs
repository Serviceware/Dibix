using System;
using System.Collections.Generic;

namespace Dibix.Dapper.Tests
{
    public sealed class ConnectionStringOptions : Dictionary<string, ConnectionStringSection>
    {
        public ConnectionStringOptions() : base(StringComparer.OrdinalIgnoreCase) { }
    }
}