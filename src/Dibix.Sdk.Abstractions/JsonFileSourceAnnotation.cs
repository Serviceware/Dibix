namespace Dibix.Sdk.Abstractions
{
    public sealed class JsonFileSourceAnnotation
    {
        public string FilePath { get; }

        public JsonFileSourceAnnotation(string filePath)
        {
            FilePath = filePath;
        }
    }
}