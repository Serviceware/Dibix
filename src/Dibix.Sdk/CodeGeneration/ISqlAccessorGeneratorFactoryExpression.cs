namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlAccessorGeneratorFactoryExpression
    {
        ISqlAccessorGeneratorBuilder Build();
        string LoadJson(string filePath);
        string ParseJson(string json);
    }
}