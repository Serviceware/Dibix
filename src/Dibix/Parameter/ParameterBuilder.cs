using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml.Linq;

namespace Dibix
{
    internal sealed class ParameterBuilder : IParameterBuilder
    {
        #region Fields
        private static readonly IDictionary<Type, DbType> TypeMap = new Dictionary<Type, DbType>
        {
            [typeof(byte)]            = DbType.Byte
          , [typeof(sbyte)]           = DbType.SByte
          , [typeof(short)]           = DbType.Int16
          , [typeof(ushort)]          = DbType.UInt16
          , [typeof(int)]             = DbType.Int32
          , [typeof(uint)]            = DbType.UInt32
          , [typeof(long)]            = DbType.Int64
          , [typeof(ulong)]           = DbType.UInt64
          , [typeof(float)]           = DbType.Single
          , [typeof(double)]          = DbType.Double
          , [typeof(decimal)]         = DbType.Decimal
          , [typeof(bool)]            = DbType.Boolean
          , [typeof(string)]          = DbType.String
          , [typeof(char)]            = DbType.StringFixedLength
          , [typeof(byte[])]          = DbType.Binary
          , [typeof(Stream)]          = DbType.Binary
          , [typeof(Uri)]             = DbType.String
          , [typeof(Guid)]            = DbType.Guid
          , [typeof(DateTime)]        = DbType.DateTime
          , [typeof(DateTimeOffset)]  = DbType.DateTimeOffset
          , [typeof(TimeSpan)]        = DbType.Time
          , [typeof(XElement)]        = DbType.Xml
          , [typeof(object)]          = DbType.Object
        };
        private static readonly IDictionary<Type, CustomInputType> CustomInputTypeMap = new Dictionary<Type, CustomInputType>
        {
            [typeof(Uri)] = Dibix.CustomInputType.Uri
        };
        private readonly IDictionary<string, Parameter> _parameters;
        #endregion

        #region Constructor
        public ParameterBuilder()
        {
            this._parameters = new Dictionary<string, Parameter>();
        }
        #endregion

        #region IParameterBuilder Members
        IParameterBuilder IParameterBuilder.SetBoolean(string parameterName, bool? parameterValue) => this.Set(parameterName, DbType.Boolean, parameterValue);
        IParameterBuilder IParameterBuilder.SetBoolean(string parameterName, out IOutParameter<bool> parameterValue) => this.Set(parameterName, DbType.Boolean, out parameterValue);
        IParameterBuilder IParameterBuilder.SetBoolean(string parameterName, out IOutParameter<bool?> parameterValue) => this.Set(parameterName, DbType.Boolean, out parameterValue);

        IParameterBuilder IParameterBuilder.SetByte(string parameterName, byte? parameterValue) => this.Set(parameterName, DbType.Byte, parameterValue);
        IParameterBuilder IParameterBuilder.SetByte(string parameterName, out IOutParameter<byte> parameterValue) => this.Set(parameterName, DbType.Byte, out parameterValue);
        IParameterBuilder IParameterBuilder.SetByte(string parameterName, out IOutParameter<byte?> parameterValue) => this.Set(parameterName, DbType.Byte, out parameterValue);

        IParameterBuilder IParameterBuilder.SetInt16(string parameterName, short? parameterValue) => this.Set(parameterName, DbType.Int16, parameterValue);
        IParameterBuilder IParameterBuilder.SetInt16(string parameterName, out IOutParameter<short> parameterValue) => this.Set(parameterName, DbType.Int16, out parameterValue);
        IParameterBuilder IParameterBuilder.SetInt16(string parameterName, out IOutParameter<short?> parameterValue) => this.Set(parameterName, DbType.Int16, out parameterValue);

        IParameterBuilder IParameterBuilder.SetInt32(string parameterName, int? parameterValue) => this.Set(parameterName, DbType.Int32, parameterValue);
        IParameterBuilder IParameterBuilder.SetInt32(string parameterName, out IOutParameter<int> parameterValue) => this.Set(parameterName, DbType.Int32, out parameterValue);
        IParameterBuilder IParameterBuilder.SetInt32(string parameterName, out IOutParameter<int?> parameterValue) => this.Set(parameterName, DbType.Int32, out parameterValue);
        
        IParameterBuilder IParameterBuilder.SetInt64(string parameterName, long? parameterValue) => this.Set(parameterName, DbType.Int64, parameterValue);
        IParameterBuilder IParameterBuilder.SetInt64(string parameterName, out IOutParameter<long> parameterValue) => this.Set(parameterName, DbType.Int64, out parameterValue);
        IParameterBuilder IParameterBuilder.SetInt64(string parameterName, out IOutParameter<long?> parameterValue) => this.Set(parameterName, DbType.Int64, out parameterValue);

        IParameterBuilder IParameterBuilder.SetSingle(string parameterName, float? parameterValue) => this.Set(parameterName, DbType.Single, parameterValue);
        IParameterBuilder IParameterBuilder.SetSingle(string parameterName, out IOutParameter<float> parameterValue) => this.Set(parameterName, DbType.Single, out parameterValue);
        IParameterBuilder IParameterBuilder.SetSingle(string parameterName, out IOutParameter<float?> parameterValue) => this.Set(parameterName, DbType.Single, out parameterValue);

        IParameterBuilder IParameterBuilder.SetDouble(string parameterName, double? parameterValue) => this.Set(parameterName, DbType.Double, parameterValue);
        IParameterBuilder IParameterBuilder.SetDouble(string parameterName, out IOutParameter<double> parameterValue) => this.Set(parameterName, DbType.Double, out parameterValue);
        IParameterBuilder IParameterBuilder.SetDouble(string parameterName, out IOutParameter<double?> parameterValue) => this.Set(parameterName, DbType.Double, out parameterValue);

        IParameterBuilder IParameterBuilder.SetDecimal(string parameterName, decimal? parameterValue) => this.Set(parameterName, DbType.Decimal, parameterValue);
        IParameterBuilder IParameterBuilder.SetDecimal(string parameterName, out IOutParameter<decimal> parameterValue) => this.Set(parameterName, DbType.Decimal, out parameterValue);
        IParameterBuilder IParameterBuilder.SetDecimal(string parameterName, out IOutParameter<decimal?> parameterValue) => this.Set(parameterName, DbType.Decimal, out parameterValue);

        IParameterBuilder IParameterBuilder.SetDateTime(string parameterName, DateTime? parameterValue) => this.Set(parameterName, DbType.DateTime, parameterValue);
        IParameterBuilder IParameterBuilder.SetDateTime(string parameterName, out IOutParameter<DateTime> parameterValue) => this.Set(parameterName, DbType.DateTime, out parameterValue);
        IParameterBuilder IParameterBuilder.SetDateTime(string parameterName, out IOutParameter<DateTime?> parameterValue) => this.Set(parameterName, DbType.DateTime, out parameterValue);

        IParameterBuilder IParameterBuilder.SetGuid(string parameterName, Guid? parameterValue) => this.Set(parameterName, DbType.Guid, parameterValue);
        IParameterBuilder IParameterBuilder.SetGuid(string parameterName, out IOutParameter<Guid> parameterValue) => this.Set(parameterName, DbType.Guid, out parameterValue);
        IParameterBuilder IParameterBuilder.SetGuid(string parameterName, out IOutParameter<Guid?> parameterValue) => this.Set(parameterName, DbType.Guid, out parameterValue);

        IParameterBuilder IParameterBuilder.SetString(string parameterName, string parameterValue) => this.SetStringCore(parameterName, parameterValue, size: null, obfuscate: false);
        IParameterBuilder IParameterBuilder.SetString(string parameterName, string parameterValue, int size) => SetStringCore(parameterName, parameterValue, size, obfuscate: false);
        IParameterBuilder IParameterBuilder.SetString(string parameterName, out IOutParameter<string> parameterValue) => this.Set(parameterName, DbType.String, out parameterValue);
        IParameterBuilder IParameterBuilder.SetString(string parameterName, string parameterValue, bool obfuscate) => this.SetStringCore(parameterName, parameterValue, size: null, obfuscate);
        IParameterBuilder IParameterBuilder.SetString(string parameterName, string parameterValue, int size, bool obfuscate) => SetStringCore(parameterName, parameterValue, size: null, obfuscate);
        private IParameterBuilder SetStringCore(string parameterName, string parameterValue, int? size, bool obfuscate)
        {
            if (parameterValue != null && obfuscate)
                parameterValue = TextObfuscator.Obfuscate(parameterValue);

            return Set(parameterName, DbType.String, parameterValue, size);
        }

        IParameterBuilder IParameterBuilder.SetBytes(string parameterName, byte[] parameterValue) => this.Set(parameterName, DbType.Binary, parameterValue);

        IParameterBuilder IParameterBuilder.SetXml(string parameterName, XElement parameterValue) => this.Set(parameterName, DbType.Xml, parameterValue);

        IParameterBuilder IParameterBuilder.SetStructured(string name, StructuredType parameterValue) => this.Set(name, DbType.Object, parameterValue);

        IParameterBuilder IParameterBuilder.SetFromTemplate(object template)
        {
            Guard.IsNotNull(template, nameof(template));
            foreach (PropertyAccessor property in TypeAccessor.GetProperties(template.GetType()))
            {
                Type outParameterType = null;
                CustomInputType customInputType = CustomInputType.None;
                DbType type = ResolveDbType(property.Type, ref outParameterType, ref customInputType);
                object value = property.GetValue(template);
                bool isObfuscated = property.IsDefined<ObfuscatedAttribute>();
                if (isObfuscated)
                {
                    SetStringCore(property.Name, (string)value, size: null, true);
                }
                else if (outParameterType == null)
                {
                    this.Set(property.Name, type, value, customInputType: customInputType);
                }
                else
                {
                    this.Set(property.Name, type, outParameterType, out OutParameter outParameter);
                    property.SetValue(template, outParameter);
                }
            }
            return this;
        }

        ParametersVisitor IParameterBuilder.Build() => new ParametersAccessor(this._parameters.Values);
        #endregion

        #region Private Methods
        private IParameterBuilder Set<T>(string name, DbType type, out IOutParameter<T> parameterValue)
        {
            OutParameter<T> outParameter = new OutParameter<T>();
            parameterValue = outParameter;
            return this.Set(name, type, value: null, outParameter: outParameter);
        }
        private IParameterBuilder Set(string name, DbType dbType, Type clrType, out OutParameter parameterValue)
        {
            OutParameter outParameter = (OutParameter)Activator.CreateInstance(typeof(OutParameter<>).MakeGenericType(clrType));
            parameterValue = outParameter;
            return this.Set(name, dbType, value: null, outParameter: outParameter);
        }
        private IParameterBuilder Set(string name, DbType type, object value, int? size = null, OutParameter outParameter = null, CustomInputType customInputType = default)
        {
            ValidateParameterLength(name, value, size);
            _parameters[name] = new Parameter(name, type, value, size, outParameter, customInputType);
            return this;
        }

        private static void ValidateParameterLength(string name, object value, int? maxLength)
        {
            if (maxLength == null)
                return;

            switch (value)
            {
                case string stringValue:
                    ValidateParameterLength(name, value, stringValue.Length, maxLength.Value);
                    break;

                case byte[] binaryValue:
                    ValidateParameterLength(name, value, binaryValue.Length, maxLength.Value);
                    break;
            }
        }
        private static void ValidateParameterLength(string name, object value, int valueLength, int maxLength)
        {
            if (valueLength > maxLength)
            {
                throw new InvalidOperationException($"""
                                                     The value for parameter '{name}' has a length of {valueLength} which exceeds the maximum length of the data type ({maxLength})
                                                     -
                                                     Value: {value}
                                                     """);
            }
        }

        private static DbType ResolveDbType(Type clrType, ref Type outParameterType, ref CustomInputType customInputType)
        {
            Type nullUnderlyingType = Nullable.GetUnderlyingType(clrType);
            if (nullUnderlyingType != null) 
                clrType = nullUnderlyingType;

            if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(IOutParameter<>))
            {
                outParameterType = clrType.GenericTypeArguments[0];
                clrType = outParameterType;
            }

            if (clrType.IsEnum && !TypeMap.ContainsKey(clrType)) 
                clrType = Enum.GetUnderlyingType(clrType);

            if (typeof(StructuredType).IsAssignableFrom(clrType))
                return DbType.Object;

            if (typeof(Stream).IsAssignableFrom(clrType))
                clrType = typeof(Stream);

            CustomInputTypeMap.TryGetValue(clrType, out customInputType);

            if (TypeMap.TryGetValue(clrType, out DbType dbType))
                return dbType;

            throw new ArgumentOutOfRangeException(nameof(clrType), clrType, null);
        }
        #endregion

        #region Nested Types
        private sealed class ParametersAccessor : ParametersVisitor
        {
            private readonly ICollection<Parameter> _parameters;

            public ParametersAccessor(ICollection<Parameter> parameters) => this._parameters = parameters;

            public override void VisitInputParameters(InputParameterVisitor visitParameter)
            {
                _parameters.Each(x => visitParameter(x.Name, x.Type, x.Value, x.Size, x.OutputParameter != null, x.CustomInputType));
            }

            public override void VisitOutputParameters(OutputParameterVisitor visitParameter)
            {
                foreach (Parameter parameter in _parameters)
                {
                    if (parameter.OutputParameter == null) 
                        continue;

                    object value = visitParameter(parameter.Name);
                    parameter.OutputParameter.ResolveValue(value);
                }
            }
        }

        private sealed class Parameter
        {
            public string Name { get; }
            public DbType Type { get; }
            public object Value { get; }
            public int? Size { get; }
            public OutParameter OutputParameter { get; }
            public CustomInputType CustomInputType { get; }

            public Parameter(string name, DbType type, object value, int? size, OutParameter outParameter, CustomInputType customInputType)
            {
                Name = name;
                Type = type;
                Value = value;
                Size = size;
                OutputParameter = outParameter;
                CustomInputType = customInputType;
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
                if (value != null)
                    this._result = (T)value;

                this._isResolved = true;
            }
        }
        #endregion
    }
}