using System;

namespace Dibix.Sdk.CodeGeneration.Model
{
    internal abstract class SchemaVisitor
    {
        private readonly ISchemaStore _schemaStore;

        protected SchemaVisitor() { }
        protected SchemaVisitor(ISchemaStore schemaStore)
        {
            this._schemaStore = schemaStore;
        }

        public virtual void Accept(SchemaDefinition node) => this.VisitCore(node);

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
        private void VisitCore(TypeReference node)
        {
            this.Visit(node);

            switch (node)
            {
                case PrimitiveTypeReference primitiveTypeReference:
                    this.VisitCore(primitiveTypeReference);
                    break;

                case SchemaTypeReference schemaTypeReference:
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
                this.VisitCore(property.Type);
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

            if (this._schemaStore != null && this._schemaStore.TryGetSchema(node.Key, out SchemaDefinition schema))
                this.VisitCore(schema);
        }
    }
}