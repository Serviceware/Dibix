using System.Xml.Linq;

namespace Dibix
{
    public interface IParameterBuilder
    {
        IParameterBuilder SetInt32(string parameterName, int? parameterValue);
        IParameterBuilder SetString(string parameterName, string parameterValue);
        IParameterBuilder SetBoolean(string parameterName, bool? parameterValue);
        IParameterBuilder SetLong(string parameterName, long? parameterValue);
        IParameterBuilder SetShort(string parameterName, short? parameterValue);
        IParameterBuilder SetByte(string parameterName, byte? parameterValue);
        IParameterBuilder SetBytes(string parameterName, byte[] parameterValue);
        IParameterBuilder SetXml(string parameterName, XElement parameterValue);
        IParameterBuilder SetStructured(string name, StructuredType value);
        IParameterBuilder SetFromTemplate(object template);
        IParametersVisitor Build();
    }
}