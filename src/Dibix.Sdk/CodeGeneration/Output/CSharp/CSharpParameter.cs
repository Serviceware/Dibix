﻿using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal sealed class CSharpParameter : CSharpStatement
    {
        private readonly string _name;
        private readonly string _type;
        private readonly CSharpValue _defaultValue;

        public CSharpParameter(string name, string type, CSharpValue defaultValue, IEnumerable<string> annotations) : base(annotations)
        {
            this._name = name;
            this._type = type;
            this._defaultValue = defaultValue;
        }

        public override void Write(StringWriter writer)
        {
            base.Write(writer);
            writer.WriteRaw(this._type)
                  .WriteRaw(' ')
                  .WriteRaw(this._name);

            if (this._defaultValue != null)
            {
                writer.WriteRaw(" = ");
                this._defaultValue.Write(writer);
            }
        }

        protected override void WriteAnnotation(StringWriter writer, string annotation)
        {
            writer.WriteRaw($"[{annotation}] ");
        }
    }
}