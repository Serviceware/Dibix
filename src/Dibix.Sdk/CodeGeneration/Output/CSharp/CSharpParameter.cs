using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal sealed class CSharpParameter : CSharpStatement
    {
        private readonly string _name;
        private readonly string _type;
        private readonly ParameterKind _parameterKind;
        private readonly CSharpValue _defaultValue;

        public CSharpParameter(string name, string type, ParameterKind parameterKind, CSharpValue defaultValue, IEnumerable<CSharpAnnotation> annotations) : base(annotations)
        {
            this._name = name;
            this._type = type;
            this._parameterKind = parameterKind;
            this._defaultValue = defaultValue;
        }

        protected override void WriteBody(StringWriter writer)
        {
            WriteParameterKind(writer, this._parameterKind);

            writer.WriteRaw(this._type)
                  .WriteRaw(' ')
                  .WriteRaw(this._name);

            WriteDefaultValue(writer, this._defaultValue);
        }

        private static void WriteParameterKind(StringWriter writer, ParameterKind parameterKind)
        {
            switch (parameterKind)
            {
                case ParameterKind.Value:
                    break;
                
                case ParameterKind.Out:
                    writer.WriteRaw("out ");
                    break;
                
                case ParameterKind.Ref:
                    writer.WriteRaw("ref ");
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterKind), parameterKind, null);
            }
        }

        private static void WriteDefaultValue(StringWriter writer, CSharpValue defaultValue)
        {
            if (defaultValue == null) 
                return;

            writer.WriteRaw(" = ");
            defaultValue.Write(writer);
        }

        protected override void WriteAnnotation(StringWriter writer, string annotation) => writer.WriteRaw($"{annotation} ");
    }
}