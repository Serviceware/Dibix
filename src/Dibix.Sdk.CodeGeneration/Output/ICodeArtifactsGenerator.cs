using System.Threading.Tasks;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal interface ICodeArtifactsGenerator
    {
        Task<bool> Generate(CodeGenerationModel model, ISchemaRegistry schemaRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry, ILogger logger);
    }
}