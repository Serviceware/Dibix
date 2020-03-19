namespace Dibix.Sdk.CodeGeneration
{
    public sealed class Error
    {
        public string Source { get; }
        public int Line { get; }
        public int Column { get; }
        public string Code { get; }
        public string Text { get; }

        public Error(string source, int line, int column, string code, string text)
        {
            this.Source = source;
            this.Line = line;
            this.Column = column;
            this.Code = code;
            this.Text = text;
        }

        public override string ToString() => $"{this.Source}({this.Line},{this.Column}) : error {this.Code}: {this.Text}";
    }
}