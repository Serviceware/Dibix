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
using System.ComponentModel;
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
        private const string GetGridCommandText = "SELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [id] = 1, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl]= NULL, [direction] = 0\r\nUNION ALL\r\nSELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [id] = 2, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl]= NULL, [direction] = 0\r\n\r\nSELECT 1";

        public static Dibix.Sdk.Tests.DomainModel.Grid.GridResult GetGrid(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                using (IMultipleResultReader reader = accessor.QueryMultiple(GetGridCommandText, CommandType.Text, ParametersVisitor.Empty))
                {
                    Dibix.Sdk.Tests.DomainModel.Grid.GridResult result = new Dibix.Sdk.Tests.DomainModel.Grid.GridResult();
                    result.Item = reader.ReadSingle<Dibix.Sdk.Tests.DomainModel.Extension.MultiMapContract>(new[] { typeof(Dibix.Sdk.Tests.DomainModel.Extension.MultiMapContract), typeof(Dibix.Sdk.Tests.DomainModel.GenericContract), typeof(Dibix.Sdk.Tests.DomainModel.Direction) }, "id,direction");
                    result.Directions.ReplaceWith(reader.ReadMany<Dibix.Sdk.Tests.DomainModel.Direction>());
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

    public enum Role : int
    {
        None,
        User,
        Admin
    }
}

namespace Dibix.Sdk.Tests.DomainModel.Extension
{
    public sealed class MultiMapContract
    {
        public System.Guid? X { get; set; }
        public IList<Dibix.Sdk.Tests.DomainModel.GenericContract> Y { get; private set; }
        public IList<Dibix.Sdk.Tests.DomainModel.Direction> Directions { get; private set; }

        public MultiMapContract()
        {
            Y = new List<Dibix.Sdk.Tests.DomainModel.GenericContract>();
            Directions = new List<Dibix.Sdk.Tests.DomainModel.Direction>();
        }
    }
}

namespace Dibix.Sdk.Tests.DomainModel.Grid
{
    public sealed class GridResult
    {
        public Dibix.Sdk.Tests.DomainModel.Extension.MultiMapContract Item { get; set; }
        public IList<Dibix.Sdk.Tests.DomainModel.Direction> Directions { get; private set; }

        public GridResult()
        {
            Directions = new List<Dibix.Sdk.Tests.DomainModel.Direction>();
        }
    }
}
#endregion