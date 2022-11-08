using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class JsonUtility
    {
        public static JToken LoadJson(string jsonFilePath)
        {
            using (Stream stream = File.OpenRead(jsonFilePath))
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(textReader))
                    {
                        return JToken.Load(jsonReader);
                    }
                }
            }
        }
    }
}