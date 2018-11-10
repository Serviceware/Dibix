using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class StringWriter
    {
        private const int TabSize = 4;
        private readonly StringBuilder _stringBuilder;
        private int _indentation;
        private int _customIndent;

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
            this._indentation++;
            return this;
        }

        public StringWriter PushCustomIndent(int customIndent)
        {
            this._customIndent = customIndent;
            return this;
        }

        public StringWriter PopIndent()
        {
            this._indentation--;
            return this;
        }

        public StringWriter PopCustomIndent()
        {
            this._customIndent = 0;
            return this;
        }

        public override string ToString()
        {
            return this._stringBuilder.ToString();
        }

        private int ComputeIndentation()
        {
            return this._indentation * TabSize + this._customIndent;
        }
    }
}