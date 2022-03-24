/*------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Dibix SDK 1.0.0.0.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//----------------------------------------------------------------------------*/
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Dibix;
using Newtonsoft.Json;

[assembly: ArtifactAssembly]
namespace Dibix.Sdk.Tests
{
    namespace Data
    {
        #region Accessor
        [DatabaseAccessor]
        public static class TestAccessor
        {
            // dbx_tests_syntax_singlemultimapresult_projection
            private const string dbx_tests_syntax_singlemultimapresult_projectionCommandText = "SELECT [id] = 1, [direction] = 0, [accessrights] = 1\r\nUNION ALL\r\nSELECT [id] = 1, [direction] = 1, [accessrights] = 1";

            public static Dibix.Sdk.Tests.DomainModel.JointContract dbx_tests_syntax_singlemultimapresult_projection(this IDatabaseAccessorFactory databaseAccessorFactory)
            {
                using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
                {
                    return accessor.QuerySingle<Dibix.Sdk.Tests.DomainModel.GenericContract, Dibix.Sdk.Tests.DomainModel.Direction, Dibix.Sdk.Tests.DomainModel.AccessRights, Dibix.Sdk.Tests.DomainModel.JointContract>(dbx_tests_syntax_singlemultimapresult_projectionCommandText, "direction,accessrights");
                }
            }
        }
        #endregion
    }

    namespace DomainModel
    {
        #region Contracts
        [Flags]
        public enum AccessRights : int
        {
            None = 0,
            Read = 1,
            Write = 2,
            Execute = 4,
            All = Read | Write | Execute
        }

        public enum Direction : int
        {
            Ascending,
            Descending
        }

        [DataContract(Namespace = "https://schemas.dibix.com/GenericContract")]
        public sealed class GenericContract
        {
            [Key]
            [DataMember]
            [JsonIgnore]
            public int Id { get; set; }
            [DataMember]
            [DefaultValue("DefaultValue")]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; } = "DefaultValue";
            [DataMember]
            [JsonIgnore]
            [Discriminator]
            public int? ParentId { get; set; }
            [DataMember]
            [DefaultValue(Dibix.Sdk.Tests.DomainModel.Role.User)]
            public Dibix.Sdk.Tests.DomainModel.Role Role { get; set; } = Dibix.Sdk.Tests.DomainModel.Role.User;
            [DataMember]
            [DateTimeKind(DateTimeKind.Utc)]
            public System.DateTime? CreationTime { get; set; }
            [DataMember]
            public System.Uri ImageUrl { get; set; }
        }

        public enum Role : int
        {
            None,
            User,
            Admin
        }

        public sealed class JointContract
        {
            public Dibix.Sdk.Tests.DomainModel.GenericContract A { get; set; }
            public Dibix.Sdk.Tests.DomainModel.Direction B { get; set; }
            public Dibix.Sdk.Tests.DomainModel.AccessRights C { get; set; }
        }
        #endregion
    }
}