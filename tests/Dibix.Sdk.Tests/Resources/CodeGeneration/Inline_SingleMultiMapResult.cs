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
using Dibix.Http;
using Newtonsoft.Json;

[assembly: ArtifactAssembly]

#region Accessor
namespace Dibix.Sdk.Tests.Data
{
    [DatabaseAccessor]
    public static class TestAccessor
    {
        // dbx_tests_syntax_singlemultimapresult
        private const string dbx_tests_syntax_singlemultimapresultCommandText = "SELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [id] = 1, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl] = NULL, [thedate] = NULL, [thetime] = NULL, [direction] = 0\r\nUNION ALL\r\nSELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [id] = 2, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl] = NULL, [thedate] = NULL, [thetime] = NULL, [direction] = 0\r\nWHERE @id = 1";

        public static Dibix.Sdk.Tests.DomainModel.Extension.MultiMapContract dbx_tests_syntax_singlemultimapresult(this IDatabaseAccessorFactory databaseAccessorFactory, int id)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create("dbx_tests_syntax_singlemultimapresult"))
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        id
                                                    })
                                                    .Build();
                return accessor.QuerySingle<Dibix.Sdk.Tests.DomainModel.Extension.MultiMapContract>(dbx_tests_syntax_singlemultimapresultCommandText, CommandType.Text, @params, new[] { typeof(Dibix.Sdk.Tests.DomainModel.Extension.MultiMapContract), typeof(Dibix.Sdk.Tests.DomainModel.GenericContract), typeof(Dibix.Sdk.Tests.DomainModel.Direction) }, "id,direction");
            }
        }
    }
}
#endregion

#region Contracts
namespace Dibix.Sdk.Tests.DomainModel
{
    [Flags]
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
        [DataMember]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public System.DateTime TheDate { get; set; }
        [DataMember]
        public System.TimeSpan TheTime { get; set; }
    }

    [Flags]
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
#endregion