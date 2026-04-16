namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public class CSharpGlobalAnnotation : CSharpAnnotation
    {
        public override bool IsGlobal => true;

        public CSharpGlobalAnnotation(string typeName, params CSharpValue[] constructorArguments) : base(typeName, constructorArguments)
        {
        }
    }

    public class CSharpGlobalAnnotation<TLeft, TRight> : CSharpGlobalAnnotation where TLeft : CSharpExpression where TRight : CSharpExpression
    {
        private readonly TLeft _left;
        private readonly TRight _right;

        public CSharpGlobalAnnotation(TLeft left, TRight right, string typeName, params CSharpValue[] constructorArguments) : base(typeName, constructorArguments)
        {
            _left = left;
            _right = right;
        }

        public override void Write(StringWriter writer)
        {
            _left.Write(writer);

            base.Write(writer);

            _right.Write(writer);
        }
    }
}