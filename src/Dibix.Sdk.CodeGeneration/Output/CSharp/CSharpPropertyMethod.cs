namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal abstract class CSharpPropertyMethod : CSharpExpression
    {
        private readonly string _name;
        private readonly string _body;
        private readonly CSharpModifiers _modifiers;

        public bool HasBody { get; }
        public bool HasMultilineBody { get; }

        public CSharpPropertyMethod(string name, string body, CSharpModifiers modifiers)
        {
            this._name = name;
            this._body = body;
            this._modifiers = modifiers;
            this.HasBody = body != null;
            this.HasMultilineBody = body != null && body.Contains("\n");
        }

        public override void Write(StringWriter writer)
        {
            WriteModifiers(writer, this._modifiers, false);
            writer.WriteRaw(this._name);

            if (this._body != null)
            {
                if (this.HasMultilineBody)
                {
                    writer.WriteLine()
                          .WriteLine("{")
                          .PushIndent();
                }
                else
                    writer.WriteRaw(" { ");

                if (this.HasMultilineBody)
                    WriteMultiline(writer, this._body);
                else
                    writer.WriteRaw(this._body);

                if (this.HasMultilineBody)
                {
                    writer.PopIndent()
                          .Write("}");
                }
                else
                    writer.WriteRaw(" }");
            }
            else
            {
                writer.WriteRaw(";");
            }
        }
    }
}