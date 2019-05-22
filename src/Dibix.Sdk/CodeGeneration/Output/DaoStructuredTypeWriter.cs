using System;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoStructuredTypeWriter : IDaoWriter
    {
        #region Properties
        public string RegionName => "Structured types";
        #endregion

        #region IDaoWriter Members
        public bool HasContent(SourceArtifacts artifacts) => artifacts.UserDefinedTypes.Any();

        public void Write(DaoWriterContext context)
        {
            context.Output.AddUsing("Dibix");
            for (int i = 0; i < context.Artifacts.UserDefinedTypes.Count; i++)
            {
                UserDefinedTypeDefinition userDefinedType = context.Artifacts.UserDefinedTypes[i];
                CSharpClass @class = context.Output
                                            .AddClass(userDefinedType.DisplayName, CSharpModifiers.Public | CSharpModifiers.Sealed)
                                            .Inherits($"StructuredType<{userDefinedType.DisplayName}, {String.Join(", ", userDefinedType.Columns.Select(x => x.Type))}>");

                @class.AddConstructor(body: $"base.ImportSqlMetadata(() => this.Add({String.Join(", ", userDefinedType.Columns.Select(x => "default"))}));"
                                    , baseConstructorParameters: $"\"{userDefinedType.TypeName}\"");

                CSharpMethod method = @class.AddMethod("Add", "void", $"base.AddValues({String.Join(", ", userDefinedType.Columns.Select(x => x.Name))});");
                foreach (UserDefinedTypeColumn column in userDefinedType.Columns)
                    method.AddParameter(column.Name, column.Type);

                if (i + 1 < context.Artifacts.Statements.Count)
                    context.Output.AddSeparator();
            }
        }
        #endregion
    }
}