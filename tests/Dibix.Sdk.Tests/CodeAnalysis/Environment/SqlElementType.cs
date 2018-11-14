using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    internal static class SqlElementType
    {
        private static readonly string[] IgnoredTypes =
        {
            nameof(ModelSchema.Column)
          , nameof(ModelSchema.AssemblySource)
          , nameof(ModelSchema.AuditAction)
          , nameof(ModelSchema.AuditActionGroup)
          , nameof(ModelSchema.AuditActionSpecification)
          , nameof(ModelSchema.ClrTypeMethod)
          , nameof(ModelSchema.ClrTypeMethodParameter)
          , nameof(ModelSchema.ClrTypeProperty)
          , nameof(ModelSchema.DatabaseMirroringLanguageSpecifier)
          , nameof(ModelSchema.DataCompressionOption)
          , nameof(ModelSchema.EventGroup)
          , nameof(ModelSchema.EventSessionAction)
          , nameof(ModelSchema.EventSessionDefinitions)
          , nameof(ModelSchema.EventSessionSetting)
          , nameof(ModelSchema.EventSessionTarget)
          , nameof(ModelSchema.EventTypeSpecifier)
          , nameof(ModelSchema.FullTextIndexColumnSpecifier)
          , nameof(ModelSchema.HttpProtocolSpecifier)
          , nameof(ModelSchema.PartitionValue)
          , nameof(ModelSchema.ServiceBrokerLanguageSpecifier)
          , nameof(ModelSchema.SignatureEncryptionMechanism)
          , nameof(ModelSchema.SoapLanguageSpecifier)
          , nameof(ModelSchema.SoapMethodSpecification)
          , nameof(ModelSchema.Parameter)
          , nameof(ModelSchema.SymmetricKeyPassword)
          , nameof(ModelSchema.TableTypeCheckConstraint)
          , nameof(ModelSchema.TableTypeColumn)
          , nameof(ModelSchema.TableTypeDefaultConstraint)
          , nameof(ModelSchema.TableTypePrimaryKeyConstraint)
          , nameof(ModelSchema.TableTypeUniqueConstraint)
          , nameof(ModelSchema.TcpProtocolSpecifier)
          , nameof(ModelSchema.XmlNamespace)
          , nameof(ModelSchema.PromotedNodePathForXQueryType)
          , nameof(ModelSchema.PromotedNodePathForSqlType)
        };

        public static IList<ModelTypeClass> Types { get; }

        static SqlElementType()
        {
            Types = typeof(ModelSchema).GetFields()
                .Where(x => typeof(ModelTypeClass).IsAssignableFrom(x.FieldType) && !IgnoredTypes.Contains(x.Name))
                .Select(x => (ModelTypeClass)x.GetValue(null))
                .ToArray();
        }
    }
}