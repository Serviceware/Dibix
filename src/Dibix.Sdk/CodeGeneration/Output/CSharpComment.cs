using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal class CSharpComment : CSharpStatement
    {
        private readonly string _comment;
        private readonly bool _isMultiline;

        public CSharpComment(string comment, bool isMultiline)
        {
            this._comment = comment;
            this._isMultiline = isMultiline;
        }

        public override void Write(StringWriter writer)
        {
            if (this._isMultiline)
            {
                writer.WriteLine("/*")
                      .PushIndent();
            }
            else
                writer.Write(String.Concat("// ", this._comment));


            if (this._isMultiline)
            {
                WriteMultiline(writer, this._comment);
                writer.PopIndent()
                      .WriteLine()
                      .Write("*/");
            }
        }
    }
}