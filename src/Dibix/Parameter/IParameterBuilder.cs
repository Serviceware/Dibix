using System;
using System.Xml.Linq;

namespace Dibix
{
    public interface IParameterBuilder
    {
        IParameterBuilder SetBoolean(string parameterName, bool? parameterValue);
        IParameterBuilder SetBoolean(string parameterName, out IOutParameter<bool> parameterValue);
        IParameterBuilder SetBoolean(string parameterName, out IOutParameter<bool?> parameterValue);

        IParameterBuilder SetByte(string parameterName, byte? parameterValue);
        IParameterBuilder SetByte(string parameterName, out IOutParameter<byte> parameterValue);
        IParameterBuilder SetByte(string parameterName, out IOutParameter<byte?> parameterValue);

        IParameterBuilder SetInt16(string parameterName, short? parameterValue);
        IParameterBuilder SetInt16(string parameterName, out IOutParameter<short> parameterValue);
        IParameterBuilder SetInt16(string parameterName, out IOutParameter<short?> parameterValue);

        IParameterBuilder SetInt32(string parameterName, int? parameterValue);
        IParameterBuilder SetInt32(string parameterName, out IOutParameter<int> parameterValue);
        IParameterBuilder SetInt32(string parameterName, out IOutParameter<int?> parameterValue);

        IParameterBuilder SetInt64(string parameterName, long? parameterValue);
        IParameterBuilder SetInt64(string parameterName, out IOutParameter<long> parameterValue);
        IParameterBuilder SetInt64(string parameterName, out IOutParameter<long?> parameterValue);

        IParameterBuilder SetSingle(string parameterName, float? parameterValue);
        IParameterBuilder SetSingle(string parameterName, out IOutParameter<float> parameterValue);
        IParameterBuilder SetSingle(string parameterName, out IOutParameter<float?> parameterValue);

        IParameterBuilder SetDouble(string parameterName, double? parameterValue);
        IParameterBuilder SetDouble(string parameterName, out IOutParameter<double> parameterValue);
        IParameterBuilder SetDouble(string parameterName, out IOutParameter<double?> parameterValue);

        IParameterBuilder SetDecimal(string parameterName, decimal? parameterValue);
        IParameterBuilder SetDecimal(string parameterName, out IOutParameter<decimal> parameterValue);
        IParameterBuilder SetDecimal(string parameterName, out IOutParameter<decimal?> parameterValue);

        IParameterBuilder SetDateTime(string parameterName, DateTime? parameterValue);
        IParameterBuilder SetDateTime(string parameterName, out IOutParameter<DateTime> parameterValue);
        IParameterBuilder SetDateTime(string parameterName, out IOutParameter<DateTime?> parameterValue);

        IParameterBuilder SetGuid(string parameterName, Guid? parameterValue);
        IParameterBuilder SetGuid(string parameterName, out IOutParameter<Guid> parameterValue);
        IParameterBuilder SetGuid(string parameterName, out IOutParameter<Guid?> parameterValue);

        IParameterBuilder SetString(string parameterName, string parameterValue);
        IParameterBuilder SetString(string parameterName, out IOutParameter<string> parameterValue);
        IParameterBuilder SetString(string parameterName, string parameterValue, bool obfuscate);

        IParameterBuilder SetBytes(string parameterName, byte[] parameterValue);
        IParameterBuilder SetXml(string parameterName, XElement parameterValue);
        IParameterBuilder SetStructured(string name, StructuredType parameterValue);
        IParameterBuilder SetFromTemplate(object template);
        ParametersVisitor Build();
    }
}