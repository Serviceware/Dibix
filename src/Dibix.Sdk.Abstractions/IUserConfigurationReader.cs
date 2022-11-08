using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.Abstractions
{
    public interface IUserConfigurationReader
    {
        void Read(string filePath, JObject json);
    }
}