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

        public bool IsGlobal { get; set; }

        public CSharpAnnotation(string typeName, params CSharpValue[] constructorArguments)
        {
            this._typeName = typeName;
            this._constructorArguments = new Collection<ConstructorArgument>();
            this._namedArguments = new Dictionary<string, CSharpValue>();

            foreach (CSharpValue constructorArgument in constructorArguments) 
                this.AddParameter(name: null, constructorArgument);
        }

        public CSharpAnnotation AddParameter(string name, CSharpValue value)
        {
            this._constructorArguments.Add(new ConstructorArgument(name, value));
            return this;
        }

        public CSharpAnnotation AddProperty(string name, CSharpValue value)
        {
            this._namedArguments.Add(name, value);
            return this;
        }

        public override void Write(StringWriter writer)
        {
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
}