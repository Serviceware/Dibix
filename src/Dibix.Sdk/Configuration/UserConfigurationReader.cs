using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk
{
    internal sealed class UserConfigurationLoader : ValidatingJsonDefinitionReader
    {
        private readonly string _filePath;
        private readonly ICollection<IUserConfigurationReader> _readers;

        protected override string SchemaName => "dibix.configuration.schema";

        public UserConfigurationLoader(string filePath, ILogger logger, params IUserConfigurationReader[] readers) : base(logger)
        {
            _filePath = filePath;
            _readers = readers;
        }

        public void Load() => Collect(Enumerable.Repeat(_filePath, 1));

        protected override void Read(JObject json)
        {
            foreach (IUserConfigurationReader reader in _readers)
            {
                reader.Read(json);
            }
        }
    }
}