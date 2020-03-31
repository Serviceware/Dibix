﻿namespace Dibix.Http
{
    public interface IHttpParameterSourceSelector
    {
        void ResolveParameterFromConstant(string targetParameterName, bool value);
        void ResolveParameterFromConstant(string targetParameterName, int value);
        void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyName);
    }
}