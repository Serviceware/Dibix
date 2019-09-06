namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal class CSharpRegionStart : CSharpStatement
    {
        private readonly string _regionName;

        public CSharpRegionStart(string regionName) => this._regionName = regionName;

        public override void Write(StringWriter writer) => writer.Write($"#region {this._regionName}");
    }
}