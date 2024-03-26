/*------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Dibix SDK 1.0.0.0.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//----------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Runtime.Serialization;
using Dibix;
using Newtonsoft.Json;

[assembly: ArtifactAssembly]

#region Accessor
namespace Dibix.Sdk.Tests.Data.Grid
{
    [DatabaseAccessor]
    public static class TestAccessor
    {
        // GetGrid
        private const string GetGridCommandText = "SELECT [id] = 1, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl]= NULL, [direction] = 0, [accessrights] = 1\r\nUNION ALL\r\nSELECT [id] = 2, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl]= NULL, [direction] = 1, [accessrights] = 1\r\n\r\nSELECT [accessrights] = 1";

        public static Dibix.Sdk.Tests.DomainModel.Grid.GetGridResult GetGrid(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                using (IMultipleResultReader reader = accessor.QueryMultiple(GetGridCommandText, CommandType.Text, ParametersVisitor.Empty))
                {
                    Dibix.Sdk.Tests.DomainModel.Grid.GetGridResult result = new Dibix.Sdk.Tests.DomainModel.Grid.GetGridResult();
                    result.Items.ReplaceWith(reader.ReadMany<Dibix.Sdk.Tests.DomainModel.JointContract>(new[] { typeof(Dibix.Sdk.Tests.DomainModel.GenericContract), typeof(Dibix.Sdk.Tests.DomainModel.Direction), typeof(Dibix.Sdk.Tests.DomainModel.AccessRights) }, "direction,accessrights"));
                    result.AccessRights = reader.ReadSingle<Dibix.Sdk.Tests.DomainModel.AccessRights>();
                    return result;
                }
            }
        }
    }
}
#endregion

#region Contracts
namespace Dibix.Sdk.Tests.DomainModel
{
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
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
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
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public System.DateTime? CreationTime { get; set; }
        [DataMember]
        public System.Uri ImageUrl { get; set; }
    }

    public sealed class JointContract
    {
        public Dibix.Sdk.Tests.DomainModel.GenericContract A { get; set; }
        public Dibix.Sdk.Tests.DomainModel.Direction B { get; set; }
        public Dibix.Sdk.Tests.DomainModel.AccessRights C { get; set; }
    }

    public enum Role : int
    {
        None,
        User,
        Admin
    }
}

namespace Dibix.Sdk.Tests.DomainModel.Grid
{
    public sealed class GetGridResult
    {
        public IList<Dibix.Sdk.Tests.DomainModel.JointContract> Items { get; private set; }
        public Dibix.Sdk.Tests.DomainModel.AccessRights AccessRights { get; set; }

        public GetGridResult()
        {
            Items = new Collection<Dibix.Sdk.Tests.DomainModel.JointContract>();
        }
    }
}
#endregion