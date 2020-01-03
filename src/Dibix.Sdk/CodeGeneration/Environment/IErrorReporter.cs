namespace Dibix.Sdk.CodeGeneration
{
    public interface IErrorReporter
    {
        bool HasErrors { get; }

        void RegisterError(string fileName, int line, int column, string errorNumber, string errorText);
    }
}