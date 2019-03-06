namespace Dibix.Sdk.CodeGeneration
{
    public interface IErrorReporter
    {
        void RegisterError(string fileName, int line, int column, string errorNumber, string errorText);
        bool ReportErrors();
    }
}