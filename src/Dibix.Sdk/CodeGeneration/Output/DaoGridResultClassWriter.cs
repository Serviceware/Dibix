using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoGridResultClassWriter : DaoWriter
    {
        #region Properties
        public override string LayerName => CodeGeneration.LayerName.DomainModel;
        public override string RegionName => "Grid result types";
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => model.Statements.Any(GenerateGridResultContract);

        public override void Write(DaoCodeGenerationContext context)
        {
            var namespaceGroups = context.Model
                                         .Statements
                                         .Where(GenerateGridResultContract)
                                         .Select(x =>
                                         {
                                             if (!(x.ResultType is SchemaTypeReference schemaTypeReference))
                                                 throw new InvalidOperationException($"Unexpected result type for grid result: {x.ResultType}");

                                             return (ObjectSchema)context.GetSchema(schemaTypeReference);
                                         })
                                         .Where(x => x != null)
                                         .GroupBy(x => context.WriteNamespaces ? NamespaceUtility.BuildRelativeNamespace(context.Model.RootNamespace, this.LayerName, x.Namespace) : null)
                                         .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, ObjectSchema> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                IList<ObjectSchema> schemas = namespaceGroup.DistinctBy(x => x.FullName).ToArray();
                for (int j = 0; j < schemas.Count; j++)
                {
                    ObjectSchema schema = schemas[j];
                    CSharpModifiers classVisibility = context.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
                    CSharpClass complexType = scope.AddClass(schema.DefinitionName, classVisibility | CSharpModifiers.Sealed);

                    IList<KeyValuePair<string, string>> collectionProperties = new Collection<KeyValuePair<string, string>>();

                    foreach (ObjectSchemaProperty property in schema.Properties)
                    {
                        string propertyTypeName = context.ResolveTypeName(property.Type);

                        if (property.Type.IsEnumerable)
                        {
                            collectionProperties.Add(new KeyValuePair<string, string>(property.Name, propertyTypeName));
                            propertyTypeName = MakeCollectionInterfaceType(propertyTypeName);
                        }

                        complexType.AddProperty(property.Name, propertyTypeName)
                                   .Getter(null)
                                   .Setter(null, property.Type.IsEnumerable ? CSharpModifiers.Private : default);
                    }

                    if (!collectionProperties.Any())
                        continue;

                    if (collectionProperties.Any())
                    {
                        context.AddUsing(typeof(ICollection<>).Namespace)
                               .AddUsing(typeof(Collection<>).Namespace);
                    }

                    StringBuilder ctorBodyWriter = new StringBuilder();
                    for (int k = 0; k < collectionProperties.Count; k++)
                    {
                        KeyValuePair<string, string> property = collectionProperties[k];
                        string collectionTypeName = MakeCollectionType(property.Value);
                        ctorBodyWriter.Append("this.")
                                      .Append(property.Key)
                                      .Append(" = new ")
                                      .Append(collectionTypeName)
                                      .Append("();");

                        if (k + 1 < collectionProperties.Count)
                            ctorBodyWriter.AppendLine();
                    }

                    complexType.AddSeparator()
                               .AddConstructor(ctorBodyWriter.ToString());

                    if (j + 1 < schemas.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < namespaceGroups.Length)
                    context.Output.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static bool GenerateGridResultContract(SqlStatementInfo statement) => statement.GenerateResultClass;

        private static string MakeCollectionInterfaceType(string typeName)
        {
            return String.Concat("ICollection<", typeName, '>');
        }

        private static string MakeCollectionType(string typeName)
        {
            return String.Concat("Collection<", typeName, '>');
        }
        #endregion
    }
}