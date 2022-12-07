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

        public UserConfigurationLoader(string filePath, IFileSystemProvider fileSystemProvider, ILogger logger, params IUserConfigurationReader[] readers) : base(fileSystemProvider, logger)
        {
            _filePath = filePath;
            _readers = readers;
        }

        public void Load() => base.Collect(Enumerable.Repeat(this._filePath, 1));

        protected override void Read(JObject json)
        {
            foreach (IUserConfigurationReader reader in _readers)
            {
                reader.Read(json);
            }
        }
    }
}