using System.Globalization;
using System.IO;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class OpenApiArtifactsGenerationUnitBase : CodeArtifactGenerationUnit
    {
        protected static string BuildOutputPath(CodeGenerationModel model, string extension) => Path.GetFullPath(Path.Combine(model.OutputDirectory, $"{model.AreaName}.{extension}"));

        protected static void WriteYaml(Stream stream, OpenApiDocument document)
        {
            using FormattingStreamWriter streamWriter = new FormattingStreamWriter(stream, CultureInfo.InvariantCulture);
            OpenApiYamlWriter yamlWriter = new NullRespectingOpenApiYamlWriter(streamWriter);
            document.Serialize(yamlWriter, OpenApiSpecVersion.OpenApi3_0);
        }

        private sealed class NullRespectingOpenApiYamlWriter : OpenApiYamlWriter
        {
            public NullRespectingOpenApiYamlWriter(FormattingStreamWriter streamWriter) : base(streamWriter) { }
            public NullRespectingOpenApiYamlWriter(FormattingStreamWriter streamWriter, OpenApiWriterSettings settings) : base(streamWriter, settings) { }

            public override void WriteNull()
            {
                // The base method says the following
                //   YAML allows null value to be represented by either nothing or the word null.
                //   We will write nothing here.
                base.WriteNull();

                // However, when reading nothing, it will be treated as an empty string ''
                // This will then cause the default to be empty string the next time it's written to file.
                // Therefore, we always write the null literal to ensure a stable output.
                Writer.Write("null");
            }
        }
    }
}