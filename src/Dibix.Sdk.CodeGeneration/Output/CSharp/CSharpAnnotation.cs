using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public class CSharpAnnotation : CSharpExpression
    {
        private readonly string _typeName;
        private readonly IList<ConstructorArgument> _constructorArguments;
        private readonly IDictionary<string, CSharpValue> _namedArguments;
        private Lazy<string> _formattedValue;

        public virtual bool IsGlobal => false;
        public int Length => _formattedValue.Value.Length;
        public virtual bool WriteIndent => true;

        public CSharpAnnotation(string typeName, params CSharpValue[] constructorArguments)
        {
            _typeName = typeName;
            _constructorArguments = new Collection<ConstructorArgument>();
            _namedArguments = new Dictionary<string, CSharpValue>();
            InitializeFormattedValueAccessor();

            foreach (CSharpValue constructorArgument in constructorArguments)
                AddParameter(name: null, constructorArgument);
        }

        public CSharpAnnotation AddParameter(string name, CSharpValue value)
        {
            InitializeFormattedValueAccessor();
            this._constructorArguments.Add(new ConstructorArgument(name, value));
            return this;
        }

        public CSharpAnnotation AddProperty(string name, CSharpValue value)
        {
            InitializeFormattedValueAccessor();
            this._namedArguments.Add(name, value);
            return this;
        }

        public override void Write(StringWriter writer)
        {
            writer.WriteRaw(_formattedValue.Value);
        }

        private void InitializeFormattedValueAccessor()
        {
            _formattedValue = new Lazy<string>(FormatValue);
        }

        private string FormatValue()
        {
            StringWriter writer = new StringWriter();
            writer.WriteRaw('[');

            if (this.IsGlobal)
                writer.WriteRaw("assembly: ");

            writer.WriteRaw(this._typeName);

            if (this._constructorArguments.Any() || this._namedArguments.Any())
                writer.WriteRaw('(');

            for (int i = 0; i < this._constructorArguments.Count; i++)
            {
                ConstructorArgument constructorArgument = this._constructorArguments[i];

                if (constructorArgument.Name != null)
                {
                    writer.WriteRaw(constructorArgument.Name)
                          .WriteRaw(": ");

                }

                constructorArgument.Value.Write(writer);

                if (i + 1 < this._constructorArguments.Count)
                    writer.WriteRaw(", ");
            }

            if (this._constructorArguments.Any() && this._namedArguments.Any())
                writer.WriteRaw(", ");

            IList<KeyValuePair<string, CSharpValue>> namedArguments = this._namedArguments.ToArray();
            for (int i = 0; i < namedArguments.Count; i++)
            {
                KeyValuePair<string, CSharpValue> namedArgument = namedArguments[i];

                writer.WriteRaw(namedArgument.Key);

                writer.WriteRaw(" = ");

                namedArgument.Value.Write(writer);

                if (i + 1 < namedArguments.Count)
                    writer.WriteRaw(", ");
            }

            if (this._constructorArguments.Any() || this._namedArguments.Any())
                writer.WriteRaw(')');

            writer.WriteRaw(']');

            string formattedValue = writer.ToString();
            return formattedValue;
        }

        private readonly struct ConstructorArgument
        {
            public string Name { get; }
            public CSharpValue Value { get; }

            public ConstructorArgument(string name, CSharpValue value)
            {
                this.Name = name;
                this.Value = value;
            }
        }
    }

    public class CSharpAnnotation<TLeft, TRight> : CSharpAnnotation where TLeft : CSharpExpression where TRight : CSharpExpression
    {
        private readonly TLeft _left;
        private readonly TRight _right;

        public override bool WriteIndent => !typeof(CSharpPreprocessorDirectiveExpression).IsAssignableFrom(typeof(TLeft));

        public CSharpAnnotation(TLeft left, TRight right, string typeName, params CSharpValue[] constructorArguments) : base(typeName, constructorArguments)
        {
            _left = left;
            _right = right;
        }

        public override void Write(StringWriter writer)
        {
            _left.Write(writer);

            writer.WriteIndent();

            base.Write(writer);

            _right.Write(writer);
        }
    }
}