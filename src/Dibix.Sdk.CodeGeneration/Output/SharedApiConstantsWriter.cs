using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SharedApiConstantsWriter : ClientWriter
    {
        #region Properties
        public override string RegionName => "Shared";
        #endregion

        #region Overrides
        public override void Write(CodeGenerationContext context)
        {
            context.Namespace()
                   .AddClass("HttpClientConstants", CSharpModifiers.Internal | CSharpModifiers.Static)
                   .AddField("BaseUrl", "string", new CSharpValue($"\"{context.Model.BaseUrl}\""), CSharpModifiers.Public | CSharpModifiers.Const);
        }
        #endregion
    }
}