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
        // AssertAuthorized
        private const string AssertAuthorizedCommandText = "[dbo].[dbx_tests_authorization]";

        // EmptyWithOutputParam
        private const string EmptyWithOutputParamCommandText = "[dbo].[dbx_tests_syntax_empty_params_out]";

        // EmptyWithParams
        private const string EmptyWithParamsCommandText = "[dbo].[dbx_tests_syntax_empty_params]";

        // EmptyWithParamsAndComplexUdt
        private const string EmptyWithParamsAndComplexUdtCommandText = "[dbo].[dbx_tests_syntax_empty_params_udt]";

        // FileResult
        private const string FileResultCommandText = "[dbo].[dbx_tests_syntax_fileresult]";

        // FileUpload
        private const string FileUploadCommandText = "[dbo].[dbx_tests_syntax_fileupload]";

        // MultiConcreteResult
        private const string MultiConcreteResultCommandText = "[dbo].[dbx_tests_syntax_multiconcreteresult]";

        // SingleConrecteResultWithArrayParam
        private const string SingleConrecteResultWithArrayParamCommandText = "[dbo].[dbx_tests_syntax_singleconcreteresult_params_array]";

        // SingleConrecteResultWithParams
        private const string SingleConrecteResultWithParamsCommandText = "[dbo].[dbx_tests_syntax_singleconcreteresult_params]";

        public static void AssertAuthorized(this IDatabaseAccessorFactory databaseAccessorFactory, byte right)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        right
                                                    })
                                                    .Build();
                accessor.Execute(AssertAuthorizedCommandText, CommandType.StoredProcedure, @params);
            }
        }

        public static short EmptyWithOutputParam(this IDatabaseAccessorFactory databaseAccessorFactory, out short a)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetInt16(nameof(a), out IOutParameter<short> aOutput)
                                                    .Build();
                short result = accessor.QuerySingle<short>(EmptyWithOutputParamCommandText, CommandType.StoredProcedure, @params);
                a = aOutput.Result;
                return result;
            }
        }

        public static void EmptyWithParams(this IDatabaseAccessorFactory databaseAccessorFactory, string a, string b, System.Guid? c, string password, Dibix.Sdk.Tests.Data.IntParameterSet ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake")
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        c,
                                                        ids,
                                                        e,
                                                        f,
                                                    })
                                                    .SetString(nameof(a), a, size: 50)
                                                    .SetString(nameof(b), b, size: 50)
                                                    .SetString(nameof(password), password, size: 128, obfuscate: true)
                                                    .SetString(nameof(d), d, size: 50)
                                                    .SetString(nameof(g), g, size: 50)
                                                    .Build();
                accessor.Execute(EmptyWithParamsCommandText, CommandType.StoredProcedure, @params);
            }
        }

        public static void EmptyWithParamsAndComplexUdt(this IDatabaseAccessorFactory databaseAccessorFactory, string a, string b, System.Guid? c, string password, Dibix.Sdk.Tests.Data.GenericParameterSet ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake")
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        c,
                                                        ids,
                                                        e,
                                                        f,
                                                    })
                                                    .SetString(nameof(a), a, size: 50)
                                                    .SetString(nameof(b), b, size: 50)
                                                    .SetString(nameof(password), password, size: 128, obfuscate: true)
                                                    .SetString(nameof(d), d, size: 50)
                                                    .SetString(nameof(g), g, size: 50)
                                                    .Build();
                accessor.Execute(EmptyWithParamsAndComplexUdtCommandText, CommandType.StoredProcedure, @params);
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

        public static Dibix.Sdk.Tests.DomainModel.GenericContract SingleConrecteResultWithArrayParam(this IDatabaseAccessorFactory databaseAccessorFactory, Dibix.Sdk.Tests.Data.IntParameterSet ids)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        ids
                                                    })
                                                    .Build();
                return accessor.QuerySingle<Dibix.Sdk.Tests.DomainModel.GenericContract>(SingleConrecteResultWithArrayParamCommandText, CommandType.StoredProcedure, @params);
            }
        }

        public static async Task<Dibix.Sdk.Tests.DomainModel.GenericContract> SingleConrecteResultWithParamsAsync(this IDatabaseAccessorFactory databaseAccessorFactory, int id, string name, CancellationToken cancellationToken = default)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        id,
                                                    })
                                                    .SetString(nameof(name), name, size: 255)
                                                    .Build();
                return await accessor.QuerySingleAsync<Dibix.Sdk.Tests.DomainModel.GenericContract>(SingleConrecteResultWithParamsCommandText, CommandType.StoredProcedure, @params, cancellationToken).ConfigureAwait(false);
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
            base.ImportSqlMetadata(() => Add(default, default));
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
            base.ImportSqlMetadata(() => Add(default));
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
    public sealed class AnotherEntry
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public sealed class AnotherInputContract
    {
        public string A { get; set; }
        public string B { get; set; }
        [Optional]
        public string C { get; set; }
        public IList<Dibix.Sdk.Tests.DomainModel.AnotherEntry> SomeIds { get; private set; }
        public System.Guid D { get; set; }
        public string Password { get; set; }
        public bool E { get; set; }
        public int F { get; set; }
        public string G { get; set; }

        public AnotherInputContract()
        {
            SomeIds = new Collection<Dibix.Sdk.Tests.DomainModel.AnotherEntry>();
        }
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

    public sealed class InputContract
    {
        public string A { get; set; }
        public string B { get; set; }
        [Optional]
        public string C { get; set; }
        public IList<Dibix.Sdk.Tests.DomainModel.Entry> Ids { get; private set; }
        public System.Guid D { get; set; }
        public string Password { get; set; }
        public bool E { get; set; }
        public int F { get; set; }
        public string G { get; set; }

        public InputContract()
        {
            Ids = new Collection<Dibix.Sdk.Tests.DomainModel.Entry>();
        }
    }

    public enum Role : int
    {
        None,
        User,
        Admin
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
            base.RegisterController("GenericEndpoint", controller =>
            {
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.MultiConcreteResult)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.SecuritySchemes.Add("DBXNS-SIT");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "{password}/Fixed";
                    action.SecuritySchemes.Add("DBXNS-SIT");
                    action.ResolveParameterFromNull<string>("password");
                    action.ResolveParameterFromSource("a", "HEADER", "User-Agent");
                    action.ResolveParameterFromSource("b", "HEADER", "Authorization.Parameter");
                    action.ResolveParameterFromSource("c", "DBX", "X", "DBX");
                    action.ResolveParameterFromSource("d", "REQUEST", "Language");
                    action.ResolveParameterFromConstant("e", true);
                    action.ResolveParameterFromConstant("f", Dibix.Sdk.Tests.DomainModel.Direction.Descending);
                    action.ResolveParameterFromConstant("g", "cake");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "{password}/User";
                    action.SecuritySchemes.Add("Anonymous");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParamsAndComplexUdt)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "UDT";
                    action.SecuritySchemes.Add("DBXNS-SIT");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(context, typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithOutputParam)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "Out";
                    action.SecuritySchemes.Add("DBXNS-SIT");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.SingleConrecteResultWithParamsAsync)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "User/{id}/{name}";
                    action.SecuritySchemes.Add("DBXNS-SIT");
                    action.SetStatusCodeDetectionResponse(404, 1, "The user '{name}' with the id '{id}' could not be found");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.SingleConrecteResultWithArrayParam)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "Array";
                    action.SecuritySchemes.Add("DBXNS-SIT");
                    action.DisableStatusCodeDetection(404);
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.FileResult)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "{id}";
                    action.SecuritySchemes.Add("Anonymous");
                    action.SecuritySchemes.Add("Bearer");
                    action.FileResponse = new HttpFileResponseDefinition(cache: false);
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.FileUpload)), action =>
                {
                    action.Method = HttpApiMethod.Put;
                    action.BodyContract = typeof(System.IO.Stream);
                    action.SecuritySchemes.Add("DBXNS-SIT");
                    action.ResolveParameterFromSource("data", "BODY", "$RAW");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Post;
                    action.BodyContract = typeof(Dibix.Sdk.Tests.DomainModel.InputContract);
                    action.SecuritySchemes.Add("DBXNS-SIT");
                    action.ResolveParameterFromBody("ids", "Dibix.GenericContractIdsInputConverter");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParamsAndComplexUdt)), action =>
                {
                    action.Method = HttpApiMethod.Patch;
                    action.BodyContract = typeof(Dibix.Sdk.Tests.DomainModel.AnotherInputContract);
                    action.SecuritySchemes.Add("DBXNS-ClientId");
                    action.SecuritySchemes.Add("DBXNS-SIT");
                    action.ResolveParameterFromSource("ids", "BODY", "SomeIds", items =>
                    {
                        items.ResolveParameterFromConstant("id", 1);
                        items.ResolveParameterFromSource("name", "ITEM", "Title");
                    });
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Delete;
                    action.SecuritySchemes.Add("DBXNS-SIT");
                    action.WithAuthorization(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.AssertAuthorized)), authorization =>
                    {
                        authorization.ResolveParameterFromConstant("right", (byte)1);
                    });
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Delete;
                    action.ChildRoute = "Alternative";
                    action.SecuritySchemes.Add("DBXNS-SIT");
                    action.WithAuthorization(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.AssertAuthorized)), authorization =>
                    {
                        authorization.ResolveParameterFromConstant("right", (byte)1);
                    });
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Delete;
                    action.ChildRoute = "AnotherAlternative";
                    action.SecuritySchemes.Add("DBXNS-SIT");
                    action.WithAuthorization(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.AssertAuthorized)), authorization =>
                    {
                        authorization.ResolveParameterFromConstant("right", (byte)1);
                    });
                });
            });
        }
    }
}
#endregion