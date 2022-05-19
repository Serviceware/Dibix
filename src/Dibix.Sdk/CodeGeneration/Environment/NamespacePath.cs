namespace Dibix.Sdk.CodeGeneration
{
    public class NamespacePath
    {
        public string ProductName { get; }
        public string AreaName { get; }
        public string LayerName { get; }
        public string RelativeNamespace { get; }
        public string Path { get; }

        public NamespacePath(string productName, string areaName, string layerName, string relativeNamespace, string path)
        {
            this.ProductName = productName;
            this.AreaName = areaName;
            this.LayerName = layerName;
            this.RelativeNamespace = relativeNamespace;
            this.Path = path;
        }

        public override string ToString() => this.Path;
    }
}