using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class SchemaVisitor
    {
        private readonly ISchemaRegistry _schemaRegistry;

        protected SchemaVisitor() { }
        protected SchemaVisitor(ISchemaRegistry schemaRegistry)
        {
            this._schemaRegistry = schemaRegistry;
        }

        public virtual void Accept(SchemaDefinition node) => this.VisitCore(node);
        public virtual void Accept(TypeReference node) => this.VisitCore(node);

        protected virtual void Visit(SchemaDefinition node) { }
        protected virtual void Visit(TypeReference node) { }
        protected virtual void Visit(EnumSchema node) { }
        protected virtual void Visit(UserDefinedTypeSchema node) { }
        protected virtual void Visit(ObjectSchema node) { }
        protected virtual void Visit(SqlStatementDefinition node) { }
        protected virtual void Visit(PrimitiveTypeReference node) { }
        protected virtual void Visit(SchemaTypeReference node) { }

        private void VisitCore(SchemaDefinition node)
        {
            this.Visit(node);

            switch (node)
            {
                case EnumSchema enumSchema:
                    this.VisitCore(enumSchema);
                    break;

                case UserDefinedTypeSchema userDefinedTypeSchema:
                    this.VisitCore(userDefinedTypeSchema);
                    break;

                case ObjectSchema objectSchema:
                    this.VisitCore(objectSchema);
                    break;

                case SqlStatementDefinition sqlStatementDefinition:
                    this.VisitCore(sqlStatementDefinition);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }
        private void VisitCore(TypeReference node, SchemaDefinition parent = null)
        {
            this.Visit(node);

            switch (node)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                    this.VisitCore(primitiveTypeReference);
                    break;

                case SchemaTypeReference schemaTypeReference:
                    // Avoid endless recursions for self referencing properties
                    if (!Equals(parent?.FullName, schemaTypeReference.Key))
                        this.VisitCore(schemaTypeReference);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }
        private void VisitCore(EnumSchema node) => this.Visit(node);
        private void VisitCore(UserDefinedTypeSchema node) => this.Visit(node);
        private void VisitCore(ObjectSchema node)
        {
            this.Visit(node);

            foreach (ObjectSchemaProperty property in node.Properties)
            {
                this.VisitCore(property.Type, node);
            }
        }
        private void VisitCore(SqlStatementDefinition node)
        {
            this.Visit(node);

            foreach (SqlQueryParameter parameter in node.Parameters)
            {
                this.VisitCore(parameter.Type);
            }

            if (node.ResultType != null)
                this.VisitCore(node.ResultType);
        }
        private void VisitCore(PrimitiveTypeReference node) => this.Visit(node);
        private void VisitCore(SchemaTypeReference node)
        {
            this.Visit(node);

            SchemaDefinition schema = this._schemaRegistry.GetSchema(node);
            if (schema != null)
                this.VisitCore(schema);
        }
    }
}