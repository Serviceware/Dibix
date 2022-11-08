using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class StringWriter
    {
        private const int TabSize = 4;
        private readonly StringBuilder _stringBuilder;
        private int _indentationIndex;
        private int _fixedIndentation;

        public StringWriter()
        {
            this._stringBuilder = new StringBuilder();
        }

        public StringWriter WriteIndent()
        {
            this._stringBuilder
                .Append(new string(' ', this.ComputeIndentation()));
            return this;
        }

        public StringWriter Write(char value)
        {
            this._stringBuilder
                .Append(new string(' ', this.ComputeIndentation()))
                .Append(value);
            return this;
        }

        public StringWriter WriteRaw(char value)
        {
            this._stringBuilder.Append(value);
            return this;
        }

        public StringWriter WriteRaw(object value)
        {
            this._stringBuilder.Append(value);
            return this;
        }

        public StringWriter Write(string value)
        {
            this._stringBuilder
                .Append(new string(' ', this.ComputeIndentation()))
                .Append(value);
            return this;
        }

        public StringWriter WriteRaw(string value)
        {
            this._stringBuilder.Append(value);
            return this;
        }

        public StringWriter WriteLine()
        {
            this._stringBuilder.AppendLine();
            return this;
        }

        public StringWriter WriteLine(string value)
        {
            this._stringBuilder
                .Append(new string(' ', this.ComputeIndentation()))
                .AppendLine(value);
            return this;
        }

        public StringWriter WriteLineRaw(string value)
        {
            this._stringBuilder.AppendLine(value);
            return this;
        }

        public StringWriter PushIndent()
        {
            this._indentationIndex++;
            return this;
        }

        public StringWriter SetTemporaryIndent(int indentSize)
        {
            this._fixedIndentation = indentSize;
            return this;
        }

        public StringWriter PopIndent()
        {
            this._indentationIndex--;
            return this;
        }

        public StringWriter ResetTemporaryIndent()
        {
            this._fixedIndentation = 0;
            return this;
        }

        public override string ToString()
        {
            return this._stringBuilder.ToString();
        }

        private int ComputeIndentation()
        {
            return this._indentationIndex * TabSize + this._fixedIndentation;
        }
    }
}