namespace Dibix.Sdk.CodeGeneration
{
    public sealed class TargetPath : NamespacePath
    {
        public string AbsoluteNamespace { get; }
        public string TargetName { get; }

        public TargetPath(string productName, string areaName, string layerName, string relativeNamespace, string absoluteNamespace, string targetName, string path) : base(productName, areaName, layerName, relativeNamespace, path)
        {
            this.AbsoluteNamespace = absoluteNamespace;
            this.TargetName = targetName;
        }
    }
}