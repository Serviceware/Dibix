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
using System.Threading;
using System.Threading.Tasks;
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
        private const string GetGridCommandText = "SELECT [id]           = 1\r\n     , [name]         = NULL\r\n     , [parentid]     = NULL\r\n     , [role]         = NULL\r\n     , [creationtime] = NULL\r\n     , [imageurl]     = NULL\r\nUNION ALL\r\nSELECT [id]           = 2\r\n     , [name]         = NULL\r\n     , [parentid]     = NULL\r\n     , [role]         = NULL\r\n     , [creationtime] = NULL\r\n     , [imageurl]     = NULL\r\n\r\nSELECT 1";

        public static async Task<Dibix.Sdk.Tests.DomainModel.Grid.GetGridResult> GetGridAsync(this IDatabaseAccessorFactory databaseAccessorFactory, CancellationToken cancellationToken = default)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                using (IMultipleResultReader reader = await accessor.QueryMultipleAsync(GetGridCommandText, CommandType.Text, ParametersVisitor.Empty, cancellationToken).ConfigureAwait(false))
                {
                    Dibix.Sdk.Tests.DomainModel.Grid.GetGridResult result = new Dibix.Sdk.Tests.DomainModel.Grid.GetGridResult();
                    result.Items.ReplaceWith(await reader.ReadManyAsync<Dibix.Sdk.Tests.DomainModel.GenericContract>().ConfigureAwait(false));
                    result.Directions.ReplaceWith(await reader.ReadManyAsync<Dibix.Sdk.Tests.DomainModel.Direction>().ConfigureAwait(false));
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
}

namespace Dibix.Sdk.Tests.DomainModel.Grid
{
    public sealed class GetGridResult
    {
        public IList<Dibix.Sdk.Tests.DomainModel.GenericContract> Items { get; private set; }
        public IList<Dibix.Sdk.Tests.DomainModel.Direction> Directions { get; private set; }

        public GetGridResult()
        {
            Items = new Collection<Dibix.Sdk.Tests.DomainModel.GenericContract>();
            Directions = new Collection<Dibix.Sdk.Tests.DomainModel.Direction>();
        }
    }
}
#endregion