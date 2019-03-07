using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IGeneratorConfigurationBuilderSourceExpression
    {
        GeneratorConfiguration Configure(Action<IGeneratorConfigurationBuilderExpression> configure);
        GeneratorConfiguration LoadJson();
        GeneratorConfiguration LoadJson(string filePath);
        GeneratorConfiguration ParseJson(string json);
    }
}