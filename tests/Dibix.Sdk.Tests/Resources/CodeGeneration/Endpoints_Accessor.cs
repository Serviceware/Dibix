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
using System.Threading;
using System.Threading.Tasks;
using Dibix;
using Dibix.Http.Server;
using Dibix.Http.Server.AspNet;
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

        public static void EmptyWithParams(this IDatabaseAccessorFactory databaseAccessorFactory, string a, string b, System.Guid? c, string? password, Dibix.Sdk.Tests.Data.IntParameterSet ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake")
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

namespace Dibix.Sdk.Tests.Data.File
{
    [DatabaseAccessor]
    public static class TestAccessor
    {
        // FileUpload
        private const string FileUploadCommandText = "[dbo].[dbx_tests_syntax_fileupload]";

        public static async Task FileUploadAsync(this IDatabaseAccessorFactory databaseAccessorFactory, System.IO.Stream data, CancellationToken cancellationToken = default)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        data
                                                    })
                                                    .Build();
                await accessor.ExecuteAsync(FileUploadCommandText, CommandType.StoredProcedure, @params, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
#endregion

#region Structured types
namespace Dibix.Sdk.Tests.Data
{
    [StructuredType("[dbo].[dbx_codeanalysis_udt_generic]")]
    public sealed class GenericParameterSet : StructuredType<GenericParameterSet>
    {
        public override string TypeName { get { return "[dbo].[dbx_codeanalysis_udt_generic]"; } }
        public void Add(int id, string? name, byte[]? data, decimal? value)
        {
            AddRecord(id, name, data, value);
        }
        protected override void CollectMetadata(ISqlMetadataCollector collector)
        {
            collector.RegisterMetadata("id", SqlDbType.Int);
            collector.RegisterMetadata("name", SqlDbType.NVarChar, 50);
            collector.RegisterMetadata("data", SqlDbType.VarBinary, 500);
            collector.RegisterMetadata("value", SqlDbType.Decimal, 15, 3);
        }
    }

    [StructuredType("[dbo].[dbx_codeanalysis_udt_int]")]
    public sealed class IntParameterSet : StructuredType<IntParameterSet>
    {
        public override string TypeName { get { return "[dbo].[dbx_codeanalysis_udt_int]"; } }
        public void Add(int id)
        {
            AddRecord(id);
        }
        protected override void CollectMetadata(ISqlMetadataCollector collector)
        {
            collector.RegisterMetadata("id", SqlDbType.Int);
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
                controller.AddAction(ExternalReflectionHttpActionTarget.Create("Dibix.Sdk.Tests.CodeGeneration.CodeGenerationTaskTests.ReflectionTarget,Dibix.Sdk.Tests"), action =>
                {
                    action.ActionName = "ReflectionTarget";
                    action.Method = HttpApiMethod.Get;
                    action.ChildRoute = "Reflection/{id}";
                    action.SecuritySchemes.Add("DibixBearer");
                    action.ResolveParameterFromSource("identifier", "DBX", "X", "DBX");
                });
            });
        }
    }
}
#endregion

#region Controller Abstractions
namespace Dibix.Sdk.Tests.Business
{
    public abstract class CodeGenerationTaskTestsBase
    {
        public Task<string> ReflectionTarget(int id, System.Guid identifier, string? name = null, int age = 18, CancellationToken cancellationToken = default)
        {
            return ReflectionTargetImplementation(id, identifier, name, age, cancellationToken);
        }

        protected abstract Task<string> ReflectionTargetImplementation(int id, System.Guid identifier, string? name, int age, CancellationToken cancellationToken);
    }
}
#endregion