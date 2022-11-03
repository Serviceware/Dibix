namespace Dibix.Http.Server
{
    public interface IHttpParameterSourceSelector
    {
        void ResolveParameterFromConstant(string targetParameterName, bool value);
        void ResolveParameterFromConstant(string targetParameterName, int value);
        void ResolveParameterFromConstant(string targetParameterName, string value);
        void ResolveParameterFromConstant<T>(string targetParameterName, T value);
        void ResolveParameterFromNull<T>(string targetParameterName);
        void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyName);
        void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyName, string converterName);
    }
}