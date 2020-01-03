namespace Dibix.Sdk.CodeGeneration
{
    public sealed class Namespace
    {
        public string FullNamespace { get; }
        public string RelativeNamespace { get; }

        private Namespace(string fullNamespace, string relativeNamespace)
        {
            this.FullNamespace = fullNamespace;
            this.RelativeNamespace = relativeNamespace;
        }

        public static Namespace Create(string productName, string areaName, string layerName, string relativeNamespace)
        {
            string fullNamespace = NamespaceUtility.BuildNamespace(productName, areaName, layerName, relativeNamespace);

            // Append layer name if project has multiple areas
            if (areaName == null)
                relativeNamespace = NamespaceUtility.BuildNamespace(null, null, layerName, relativeNamespace);

            return new Namespace(fullNamespace, relativeNamespace);
        }
    }
}