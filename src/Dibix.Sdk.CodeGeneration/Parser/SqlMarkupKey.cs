namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlMarkupKey : IdMap<SqlMarkupKey, string>
    {
        public const string Namespace = nameof(Namespace);
        public const string Name = nameof(Name);
        public const string NoCompile = nameof(NoCompile);
        public const string MergeGridResult = nameof(MergeGridResult);
        public const string FileResult = nameof(FileResult);
        public const string Async = nameof(Async);
        public const string Return = nameof(Return);
        public const string ResultTypeName = nameof(ResultTypeName);
        public const string GeneratedResultTypeName = nameof(GeneratedResultTypeName);
        public const string GenerateInputClass = nameof(GenerateInputClass);
        public const string ClrType = nameof(ClrType);
        public const string Obfuscate = nameof(Obfuscate);
        public const string Enum = nameof(Enum);
    }
}