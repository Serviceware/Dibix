using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;

namespace Dibix
{
    public sealed class ParameterBuilder : IParameterBuilder
    {
        #region Fields
        private readonly IDictionary<string, ParameterValue> _parameters;
        #endregion

        #region Constructor
        public ParameterBuilder()
        {
            this._parameters = new Dictionary<string, ParameterValue>();
        }
        #endregion

        #region IParameterBuilder Members
        IParameterBuilder IParameterBuilder.SetBoolean(string parameterName, bool? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetByte(string parameterName, byte? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetInt16(string parameterName, short? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetInt32(string parameterName, int? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetInt64(string parameterName, long? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetSingle(string parameterName, float? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetDouble(string parameterName, double? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetDecimal(string parameterName, decimal? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetDateTime(string parameterName, DateTime? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetString(string parameterName, string parameterValue, bool obfuscate)
        {
            if (parameterValue != null && obfuscate)
                parameterValue = TextObfuscator.Obfuscate(parameterValue);

            return this.Set(parameterName, parameterValue);
        }

        IParameterBuilder IParameterBuilder.SetBytes(string parameterName, byte[] parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetXml(string parameterName, XElement parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetStructured(string name, StructuredType parameterValue) => this.Set(name, parameterValue);

        IParameterBuilder IParameterBuilder.SetFromTemplate(object template)
        {
            Guard.IsNotNull(template, nameof(template));
            foreach (PropertyAccessor property in TypeAccessor.GetProperties(template.GetType()))
            {
                object value = property.GetValue(template);
                this.Set(property.Name, property.Type, value);
            }
            return this;
        }

        IParametersVisitor IParameterBuilder.Build() => new ParametersAccessor(this._parameters);
        #endregion

        #region Private Methods
        private IParameterBuilder Set<T>(string name, T value) { return this.Set(name, typeof(T), value); }
        private IParameterBuilder Set(string name, Type type, object value)
        {
            type = NormalizeType(type);
            DbType? suggestedDataType = TryOverrideDataType(type);
            this._parameters[name] = new ParameterValue(type, value, suggestedDataType);
            return this;
        }

        private static Type NormalizeType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return Nullable.GetUnderlyingType(type);

            return type;
        }

        private static DbType? TryOverrideDataType(Type type)
        {
            if (type == typeof(byte[]))
                return DbType.Binary;

            if (type == typeof(XElement) || type == typeof(XDocument))
                return DbType.Xml;

            return null;
        }
        #endregion

        #region Nested Types
        private class ParametersAccessor : IParametersVisitor
        {
            private readonly IDictionary<string, ParameterValue> _parameters;

            public ParametersAccessor(IDictionary<string, ParameterValue> parameters)
            {
                this._parameters = parameters;
            }

            void IParametersVisitor.VisitParameters(ParameterVisitor visitParameter)
            {
                this._parameters.Each(x => visitParameter(x.Key, x.Value.Value, x.Value.Type, x.Value.SuggestedDataType));
            }
        }

        private class ParameterValue
        {
            public Type Type { get; }
            public object Value { get; }
            public DbType? SuggestedDataType { get; }

            public ParameterValue(Type type, object value, DbType? suggestedDataType)
            {
                this.Type = type;
                this.Value = value;
                this.SuggestedDataType = suggestedDataType;
            }
        }
        #endregion
    }
}