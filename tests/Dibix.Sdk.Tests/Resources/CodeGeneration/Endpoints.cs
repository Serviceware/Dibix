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
        // AssertAuthorized
        private const string AssertAuthorizedCommandText = "[dbo].[dbx_tests_authorization]";

        // EmptyWithParams
        private const string EmptyWithParamsCommandText = "[dbo].[dbx_tests_syntax_empty_params]";

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

        public static void EmptyWithParams(this IDatabaseAccessorFactory databaseAccessorFactory, string a, string b, System.Guid? c, string password, Dibix.Sdk.Tests.Data.GenericParameterSet ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake")
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        a,
                                                        b,
                                                        c,
                                                        ids,
                                                        d,
                                                        e,
                                                        f,
                                                        g
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

        public static Dibix.Sdk.Tests.DomainModel.GenericContract SingleConrecteResultWithParams(this IDatabaseAccessorFactory databaseAccessorFactory, int id, string name)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        id,
                                                        name
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
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator) => actionDelegator.Delegate(new Dictionary<string, object>()));
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "{password}/Fixed";
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, string password, string userAgent, string authorization, Dibix.Sdk.Tests.Data.GenericParameterSet ids, string? acceptLanguage) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "password", password },
                        { "a", userAgent },
                        { "b", authorization },
                        { "ids", ids },
                        { "d", acceptLanguage }
                    }));
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
                    action.IsAnonymous = true;
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, string password, string a, string b, System.Guid? c, Dibix.Sdk.Tests.Data.GenericParameterSet ids, string? d, bool e, Dibix.Sdk.Tests.DomainModel.Direction? f, string? g) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "password", password },
                        { "a", a },
                        { "b", b },
                        { "c", c },
                        { "ids", ids },
                        { "d", d },
                        { "e", e },
                        { "f", f },
                        { "g", g }
                    }));
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.SingleConrecteResultWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "User/{id}/{name}";
                    action.SetStatusCodeDetectionResponse(404, 1, "The user '{name}' with the id '{id}' could not be found");
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, int id, string name) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "id", id },
                        { "name", name }
                    }));
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.SingleConrecteResultWithArrayParam)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "Array";
                    action.DisableStatusCodeDetection(404);
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, Dibix.Sdk.Tests.Data.IntParameterSet ids) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "ids", ids }
                    }));
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.FileResult)), action =>
                {
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "{id}";
                    action.IsAnonymous = true;
                    action.FileResponse = new HttpFileResponseDefinition(cache: false);
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, int id) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "id", id }
                    }));
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.FileUpload)), action =>
                {
                    action.Method = HttpApiMethod.Put;
                    action.BodyContract = typeof(System.IO.Stream);
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, System.IO.Stream body) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "body", body }
                    }));
                    action.ResolveParameterFromSource("data", "BODY", "$RAW");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Post;
                    action.BodyContract = typeof(Dibix.Sdk.Tests.DomainModel.InputContract);
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, Dibix.Sdk.Tests.DomainModel.InputContract body) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "body", body }
                    }));
                    action.ResolveParameterFromBody("ids", "Dibix.GenericContractIdsInputConverter");
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Patch;
                    action.BodyContract = typeof(Dibix.Sdk.Tests.DomainModel.AnotherInputContract);
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, Dibix.Sdk.Tests.DomainModel.AnotherInputContract body) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "body", body }
                    }));
                    action.ResolveParameterFromSource("ids", "BODY", "SomeIds", items =>
                    {
                        items.ResolveParameterFromConstant("id", 1);
                        items.ResolveParameterFromSource("name", "ITEM", "Title");
                    });
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Delete;
                    action.WithAuthorization(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.AssertAuthorized)), authorization =>
                    {
                        authorization.ResolveParameterFromConstant("right", (byte)1);
                    });
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, string a, string b, System.Guid? c, string password, Dibix.Sdk.Tests.Data.GenericParameterSet ids, string? d, bool e, Dibix.Sdk.Tests.DomainModel.Direction? f, string? g) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "a", a },
                        { "b", b },
                        { "c", c },
                        { "password", password },
                        { "ids", ids },
                        { "d", d },
                        { "e", e },
                        { "f", f },
                        { "g", g }
                    }));
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Delete;
                    action.ChildRoute = "Alternative";
                    action.WithAuthorization(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.AssertAuthorized)), authorization =>
                    {
                        authorization.ResolveParameterFromConstant("right", (byte)1);
                    });
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, string a, string b, System.Guid? c, string password, Dibix.Sdk.Tests.Data.GenericParameterSet ids, string? d, bool e, Dibix.Sdk.Tests.DomainModel.Direction? f, string? g) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "a", a },
                        { "b", b },
                        { "c", c },
                        { "password", password },
                        { "ids", ids },
                        { "d", d },
                        { "e", e },
                        { "f", f },
                        { "g", g }
                    }));
                });
                controller.AddAction(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.EmptyWithParams)), action =>
                {
                    action.Method = HttpApiMethod.Delete;
                    action.ChildRoute = "AnotherAlternative";
                    action.WithAuthorization(ReflectionHttpActionTarget.Create(typeof(Dibix.Sdk.Tests.Data.TestAccessor), nameof(Dibix.Sdk.Tests.Data.TestAccessor.AssertAuthorized)), authorization =>
                    {
                        authorization.ResolveParameterFromConstant("right", (byte)1);
                    });
                    action.RegisterDelegate((IHttpActionDelegator actionDelegator, string a, string b, System.Guid? c, string password, Dibix.Sdk.Tests.Data.GenericParameterSet ids, string? d, bool e, Dibix.Sdk.Tests.DomainModel.Direction? f, string? g) => actionDelegator.Delegate(new Dictionary<string, object>
                    {
                        { "a", a },
                        { "b", b },
                        { "c", c },
                        { "password", password },
                        { "ids", ids },
                        { "d", d },
                        { "e", e },
                        { "f", f },
                        { "g", g }
                    }));
                });
            });
        }
    }
}
#endregion