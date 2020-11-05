using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;

namespace Dibix
{
    public sealed class ParameterBuilder : IParameterBuilder
    {
        #region Fields
        private readonly IDictionary<string, Parameter> _parameters;
        #endregion

        #region Constructor
        public ParameterBuilder()
        {
            this._parameters = new Dictionary<string, Parameter>();
        }
        #endregion

        #region IParameterBuilder Members
        IParameterBuilder IParameterBuilder.SetBoolean(string parameterName, bool? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetByte(string parameterName, byte? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetInt16(string parameterName, short? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetInt32(string parameterName, int? parameterValue) => this.Set(parameterName, parameterValue);
        
        IParameterBuilder IParameterBuilder.SetInt32(string parameterName, out IOutParameter<int> parameterValue) => this.Set(parameterName, DbType.Int32, out parameterValue);

        IParameterBuilder IParameterBuilder.SetInt64(string parameterName, long? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetSingle(string parameterName, float? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetDouble(string parameterName, double? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetDecimal(string parameterName, decimal? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetDateTime(string parameterName, DateTime? parameterValue) => this.Set(parameterName, parameterValue);

        IParameterBuilder IParameterBuilder.SetGuid(string parameterName, out IOutParameter<Guid> parameterValue) => this.Set(parameterName, DbType.Guid, out parameterValue);

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
                this.Set(name: property.Name, dataType: null, type: property.Type, value: value, outParameter: null);
            }
            return this;
        }

        IParametersVisitor IParameterBuilder.Build() => new ParametersAccessor(this._parameters.Values);
        #endregion

        #region Private Methods
        private IParameterBuilder Set<T>(string name, T value) => this.Set(name, null, typeof(T), value, null);
        private IParameterBuilder Set<T>(string parameterName, DbType dataType, out IOutParameter<T> parameterValue)
        {
            OutParameter<T> outParameter = new OutParameter<T>();
            parameterValue = outParameter;
            return this.Set(parameterName, dataType, typeof(T), null, outParameter);
        }
        private IParameterBuilder Set(string name, DbType? dataType, Type type, object value, OutParameter outParameter)
        {
            type = NormalizeType(type);
            DbType? suggestedDataType = TryOverrideDataType(dataType, type);
            this._parameters[name] = new Parameter(name, type, value, suggestedDataType, outParameter);
            return this;
        }

        private static Type NormalizeType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return Nullable.GetUnderlyingType(type);

            return type;
        }

        private static DbType? TryOverrideDataType(DbType? dataType, Type type)
        {
            if (type == typeof(byte[]))
                return DbType.Binary;

            if (type == typeof(XElement) || type == typeof(XDocument))
                return DbType.Xml;

            return dataType;
        }
        #endregion

        #region Nested Types
        private sealed class ParametersAccessor : IParametersVisitor
        {
            private readonly ICollection<Parameter> _parameters;

            public ParametersAccessor(ICollection<Parameter> parameters)
            {
                this._parameters = parameters;
            }

            void IParametersVisitor.VisitInputParameters(InputParameterVisitor visitParameter)
            {
                this._parameters.Each(x => visitParameter(x.Name, x.Value, x.Type, x.SuggestedDataType, x.OutputParameter != null));
            }

            public void VisitOutputParameters(OutputParameterVisitor visitParameter)
            {
                this._parameters
                    .Where(x => x.OutputParameter != null)
                    .Each(x => x.OutputParameter.ResolveValue(visitParameter(x.Name)));
            }
        }

        private sealed class Parameter
        {
            public string Name { get; }
            public Type Type { get; }
            public object Value { get; }
            public DbType? SuggestedDataType { get; }
            public OutParameter OutputParameter { get; }

            public Parameter(string name, Type type, object value, DbType? suggestedDataType, OutParameter outParameter)
            {
                this.Name = name;
                this.Type = type;
                this.Value = value;
                this.SuggestedDataType = suggestedDataType;
                this.OutputParameter = outParameter;
            }
        }

        private abstract class OutParameter
        {
            public abstract void ResolveValue(object value);
        }

        private sealed class OutParameter<T> : OutParameter, IOutParameter<T>
        {
            private bool _isResolved;
            private T _result;

            public T Result
            {
                get
                {
                    if (!this._isResolved)
                        throw new InvalidOperationException("Value is not resolved yet");

                    return this._result;
                }
            }

            public override void ResolveValue(object value)
            {
                this._result = (T)value;
                this._isResolved = true;
            }
        }
        #endregion
    }
}