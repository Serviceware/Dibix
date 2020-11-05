using System;
using System.Xml.Linq;

namespace Dibix
{
    public interface IParameterBuilder
    {
        IParameterBuilder SetBoolean(string parameterName, bool? parameterValue);
        IParameterBuilder SetByte(string parameterName, byte? parameterValue);
        IParameterBuilder SetInt16(string parameterName, short? parameterValue);
        IParameterBuilder SetInt32(string parameterName, int? parameterValue);
        IParameterBuilder SetInt32(string parameterName, out IOutParameter<int> parameterValue);
        IParameterBuilder SetInt64(string parameterName, long? parameterValue);
        IParameterBuilder SetSingle(string parameterName, float? parameterValue);
        IParameterBuilder SetDouble(string parameterName, double? parameterValue);
        IParameterBuilder SetDecimal(string parameterName, decimal? parameterValue);
        IParameterBuilder SetDateTime(string parameterName, DateTime? parameterValue);
        IParameterBuilder SetGuid(string parameterName, out IOutParameter<Guid> parameterValue);
        IParameterBuilder SetString(string parameterName, string parameterValue, bool obfuscate = false);
        IParameterBuilder SetBytes(string parameterName, byte[] parameterValue);
        IParameterBuilder SetXml(string parameterName, XElement parameterValue);
        IParameterBuilder SetStructured(string name, StructuredType parameterValue);
        IParameterBuilder SetFromTemplate(object template);
        IParametersVisitor Build();
    }
}