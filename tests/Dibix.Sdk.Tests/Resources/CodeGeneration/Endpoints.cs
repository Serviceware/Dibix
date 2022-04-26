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
using Dibix.Http.Server;
using Newtonsoft.Json;

[assembly: ArtifactAssembly]
[assembly: AreaRegistration("Tests")]

#region Accessor
namespace Dibix.Sdk.Tests.Data
{
    [DatabaseAccessor]
    public static class TestAccessor
    {
        // EmptyWithParams
        private const string EmptyWithParamsCommandText = "[dbo].[dbx_tests_syntax_empty_params]";

        // FileResult
        private const string FileResultCommandText = "[dbo].[dbx_tests_syntax_fileresult]";

        // FileUpload
        private const string FileUploadCommandText = "[dbo].[dbx_tests_syntax_fileupload]";

        // MultiConcreteResult
        private const string MultiConcreteResultCommandText = "[dbo].[dbx_tests_syntax_multiconcreteresult]";

        // SingleConrecteResultWithParams
        private const string SingleConrecteResultWithParamsCommandText = "[dbo].[dbx_tests_syntax_singleconcreteresult_params]";

        public static void EmptyWithParams(this IDatabaseAccessorFactory databaseAccessorFactory, string u, string v, System.Guid? w, string password, Dibix.Sdk.Tests.Data.GenericParameterSet ids, string? x = null, bool y = true, Dibix.Sdk.Tests.DomainModel.Direction? z = null)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        u,
                                                        v,
                                                        w,
                                                        ids,
                                                        x,
                                                        y,
                                                        z
                                                    })
                                                    .SetString(nameof(password), password, true)
                                                    .Build();
                accessor.Execute(EmptyWithParamsCommandText, CommandType.StoredProcedure, @params);
            }
        }

        public static Dibix.FileEntity FileResult(this IDatabaseAccessorFactory databaseAccessorFactory, int id)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        id
                                                    })
                                                    .Build();
                return accessor.QuerySingleOrDefault<Dibix.FileEntity>(FileResultCommandText, CommandType.StoredProcedure, @params);
            }
        }

        public static void FileUpload(this IDatabaseAccessorFactory databaseAccessorFactory, System.IO.Stream data)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        data
                                                    })
                                                    .Build();
                accessor.Execute(FileUploadCommandText, CommandType.StoredProcedure, @params);
            }
        }

        public static IEnumerable<Dibix.Sdk.Tests.DomainModel.GenericContract> MultiConcreteResult(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                return accessor.QueryMany<Dibix.Sdk.Tests.DomainModel.GenericContract>(MultiConcreteResultCommandText, CommandType.StoredProcedure, ParametersVisitor.Empty);
            }
        }

        public static Dibix.Sdk.Tests.DomainModel.GenericContract SingleConrecteResultWithParams(this IDatabaseAccessorFactory databaseAccessorFactory, Dibix.Sdk.Tests.Data.IntParameterSet ids)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        ids
                                                    })
                                                    .Build();
                return accessor.QuerySingle<Dibix.Sdk.Tests.DomainModel.GenericContract>(SingleConrecteResultWithParamsCommandText, CommandType.StoredProcedure, @params);
            }
        }
    }
}
#endregion

#region Structured types
namespace Dibix.Sdk.Tests.Data
{
    [StructuredType("[dbo].[dbx_codeanalysis_udt_generic]")]
    public sealed class GenericParameterSet : StructuredType<GenericParameterSet, int, string?>
    {
        public GenericParameterSet() : base("[dbo].[dbx_codeanalysis_udt_generic]")
        {
            base.ImportSqlMetadata(() => this.Add(default, default));
        }
        public void Add(int id, string? name)
        {
            base.AddValues(id, name);
        }
    }

    [StructuredType("[dbo].[dbx_codeanalysis_udt_int]")]
    public sealed class IntParameterSet : StructuredType<IntParameterSet, int>
    {
        public IntParameterSet() : base("[dbo].[dbx_codeanalysis_udt_int]")
        {
            base.ImportSqlMetadata(() => this.Add(default));
        }
        public void Add(int id)
        {
            base.AddValues(id);
        }
    }
}
#endregion

#region Contracts
namespace Dibix.Sdk.Tests.DomainModel
{
    public sealed class AnotherInputContract
    {
        public string U { get; set; }
        public string V { get; set; }
        [Optional]
        public string W { get; set; }
        public IList<Dibix.Sdk.Tests.DomainModel.AnotherEntry> SomeIds { get; private set; }
        public System.Guid X { get; set; }
        public string Password { get; set; }
        public bool Y { get; set; }
        public int Z { get; set; }

        public AnotherInputContract()
        {
            this.SomeIds = new Collection<Dibix.Sdk.Tests.DomainModel.AnotherEntry>();
        }
    }

    public sealed class AnotherEntry
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public enum Direction : int
    {
        Ascending,
        Descending
    }

    public sealed class Entry
    {
        public int Id { get; set; }
        public string Name { get; set; }
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

    public sealed class InputContract
    {
        public string U { get; set; }
        public string V { get; set; }
        [Optional]
        public string W { get; set; }
        public IList<Dibix.Sdk.Tests.DomainModel.Entry> Ids { get; private set; }
        public System.Guid X { get; set; }
        public string Password { get; set; }
        public bool Y { get; set; }
        public int Z { get; set; }

        public InputContract()
        {
            this.Ids = new Collection<Dibix.Sdk.Tests.DomainModel.Entry>();
        }
    }
}
#endregion

#region Endpoints
namespace Dibix.Sdk.Tests.Business
{
    public sealed class ApiConfiguration : HttpApiDescriptor
    {
        public override void Configure(IHttpApiDiscoveryContext context)
        {
            base.RegisterController("GenericEndpoint", x => 
            {
                x.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.MultiConcreteResult)), y =>
                {
                    y.Method = HttpApiMethod.Get;
                });
                x.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), y =>
                {
                    y.Method = HttpApiMethod.Get;
                    y.ChildRoute = "{password}/Fixed";
                    y.ResolveParameterFromNull("password");
                    y.ResolveParameterFromSource("u", "HEADER", "User-Agent");
                    y.ResolveParameterFromSource("v", "HEADER", "Authorization.Parameter");
                    y.ResolveParameterFromSource("w", "DBX", "X", "DBX");
                    y.ResolveParameterFromSource("x", "REQUEST", "Language");
                    y.ResolveParameterFromConstant("y", true);
                    y.ResolveParameterFromConstant("z", 5);
                });
                x.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), y =>
                {
                    y.Method = HttpApiMethod.Get;
                    y.ChildRoute = "{password}/User";
                    y.IsAnonymous = true;
                });
                x.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.SingleConrecteResultWithParams)), y =>
                {
                    y.Method = HttpApiMethod.Get;
                    y.ChildRoute = "Array";
                });
                x.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.FileResult)), y =>
                {
                    y.Method = HttpApiMethod.Get;
                    y.ChildRoute = "{id}";
                    y.IsAnonymous = true;
                    y.FileResponse = new HttpFileResponseDefinition(cache: false);
                });
                x.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.FileUpload)), y =>
                {
                    y.Method = HttpApiMethod.Put;
                    y.BodyContract = typeof(System.IO.Stream);
                    y.ResolveParameterFromSource("data", "BODY", "$RAW");
                });
                x.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), y =>
                {
                    y.Method = HttpApiMethod.Post;
                    y.BodyContract = typeof(Dibix.Sdk.Tests.DomainModel.InputContract);
                    y.ResolveParameterFromBody("ids", "Dibix.GenericContractIdsInputConverter");
                });
                x.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), y =>
                {
                    y.Method = HttpApiMethod.Patch;
                    y.BodyContract = typeof(Dibix.Sdk.Tests.DomainModel.InputContract);
                });
                x.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), y =>
                {
                    y.Method = HttpApiMethod.Delete;
                    y.BodyContract = typeof(Dibix.Sdk.Tests.DomainModel.AnotherInputContract);
                    y.ResolveParameterFromSource("ids", "BODY", "SomeIds", z => 
                    {
                        z.ResolveParameterFromSource("name", "ITEM", "Title");
                    });
                });
            });
        }
    }
}
#endregion