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
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Client;
using UriBuilder = Dibix.Http.Client.UriBuilder;

[assembly: ArtifactAssembly]

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
        public string C { get; set; }
        public IList<Dibix.Sdk.Tests.DomainModel.AnotherEntry> SomeIds { get; private set; }
        public System.Guid D { get; set; }
        public string Password { get; set; }
        public bool E { get; set; }
        public int F { get; set; }
        public Dibix.Sdk.Tests.DomainModel.AnotherInputContractData Data { get; set; }

        public AnotherInputContract()
        {
            SomeIds = new Collection<Dibix.Sdk.Tests.DomainModel.AnotherEntry>();
        }
    }

    public sealed class AnotherInputContractData
    {
        public string Name { get; set; }
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

    public sealed class GenericContract
    {
        [DefaultValue("DefaultValue")]
        public string Name { get; set; } = "DefaultValue";
        [DefaultValue(Dibix.Sdk.Tests.DomainModel.Role.User)]
        public Dibix.Sdk.Tests.DomainModel.Role Role { get; set; } = Dibix.Sdk.Tests.DomainModel.Role.User;
        public System.DateTime? CreationTime { get; set; }
        [RelativeHttpsUrl]
        public System.Uri ImageUrl { get; set; }
    }

    public sealed class InputContract
    {
        public string A { get; set; }
        public string B { get; set; }
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

#region Interfaces
namespace Dibix.Sdk.Tests.Client
{
    public interface IGenericEndpointService : IHttpService
    {
        Task<HttpResponse<short>> EmptyWithOutputParamAsync(CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParams1Async(string? password, string userAgent, IEnumerable<int> ids, string? acceptLanguage = null, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParams2Async(Dibix.Sdk.Tests.DomainModel.InputContract body, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParams3Async(string a, string b, IEnumerable<int> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParams4Async(string a, string b, IEnumerable<int> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParams5Async(string a, string b, IEnumerable<int> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParamsAndComplexUdtAsync(Dibix.Sdk.Tests.DomainModel.AnotherInputContract body, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParamsAnonymousAsync(string? password, string a, string b, IEnumerable<int> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default);
        Task<HttpResponse<System.IO.Stream>> FileResultAsync(int id, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> FileUploadAsync(System.IO.Stream body, CancellationToken cancellationToken = default);
        Task<HttpResponse<IReadOnlyList<Dibix.Sdk.Tests.DomainModel.GenericContract>>> MultiConcreteResultAsync(CancellationToken cancellationToken = default);
        Task<HttpResponse<string>> ReflectionTargetAsync(int id, string? name = null, int age = 18, CancellationToken cancellationToken = default);
        Task<HttpResponse<Dibix.Sdk.Tests.DomainModel.GenericContract>> SingleConrecteResultWithArrayParamAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<HttpResponse<Dibix.Sdk.Tests.DomainModel.GenericContract>> SingleConrecteResultWithParamsAsync(int id, string name, CancellationToken cancellationToken = default);
    }
}
#endregion

#region Implementation
namespace Dibix.Sdk.Tests.Client
{
    [HttpService(typeof(IGenericEndpointService))]
    public sealed class GenericEndpointService : IGenericEndpointService
    {
        private static readonly MediaTypeFormatter Formatter = new JsonMediaTypeFormatter();
        private readonly string _httpClientName;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClientOptions _httpClientOptions;
        private readonly IHttpAuthorizationProvider _httpAuthorizationProvider;

        public GenericEndpointService(IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider httpAuthorizationProvider, string httpClientName) : this(httpClientFactory, HttpClientOptions.Default, httpAuthorizationProvider, httpClientName) { }
        public GenericEndpointService(IHttpClientFactory httpClientFactory, HttpClientOptions httpClientOptions, IHttpAuthorizationProvider httpAuthorizationProvider, string httpClientName)
        {
            _httpClientFactory = httpClientFactory;
            _httpClientOptions = httpClientOptions;
            _httpAuthorizationProvider = httpAuthorizationProvider;
            _httpClientName = httpClientName;
        }

        public async Task<HttpResponse<short>> EmptyWithOutputParamAsync(CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "Tests/GenericEndpoint/Out");
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                short responseContent = await responseMessage.Content.ReadAsAsync<short>(MediaTypeFormattersFactory.Create(_httpClientOptions, client), cancellationToken).ConfigureAwait(false);
                return new HttpResponse<short>(responseMessage, responseContent);
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParams1Async(string? password, string userAgent, IEnumerable<int> ids, string? acceptLanguage = null, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                Uri uri = UriBuilder.Create($"Tests/GenericEndpoint/{password}/Fixed", UriKind.Relative)
                                    .AddQueryParam(nameof(ids), ids)
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), uri);
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                requestMessage.Headers.Add("User-Agent", userAgent);
                if (acceptLanguage != null)
                    requestMessage.Headers.Add("Accept-Language", acceptLanguage);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParams2Async(Dibix.Sdk.Tests.DomainModel.InputContract body, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "Tests/GenericEndpoint");
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                requestMessage.Content = new ObjectContent<Dibix.Sdk.Tests.DomainModel.InputContract>(body, Formatter);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParams3Async(string a, string b, IEnumerable<int> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                Uri uri = UriBuilder.Create("Tests/GenericEndpoint", UriKind.Relative)
                                    .AddQueryParam(nameof(a), a)
                                    .AddQueryParam(nameof(b), b)
                                    .AddQueryParam(nameof(ids), ids)
                                    .AddQueryParam(nameof(d), d, null)
                                    .AddQueryParam(nameof(e), e, true)
                                    .AddQueryParam(nameof(f), f, null)
                                    .AddQueryParam(nameof(g), g, "Cake")
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("DELETE"), uri);
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParams4Async(string a, string b, IEnumerable<int> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                Uri uri = UriBuilder.Create("Tests/GenericEndpoint/Alternative", UriKind.Relative)
                                    .AddQueryParam(nameof(a), a)
                                    .AddQueryParam(nameof(b), b)
                                    .AddQueryParam(nameof(ids), ids)
                                    .AddQueryParam(nameof(d), d, null)
                                    .AddQueryParam(nameof(e), e, true)
                                    .AddQueryParam(nameof(f), f, null)
                                    .AddQueryParam(nameof(g), g, "Cake")
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("DELETE"), uri);
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParams5Async(string a, string b, IEnumerable<int> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                Uri uri = UriBuilder.Create("Tests/GenericEndpoint/AnotherAlternative", UriKind.Relative)
                                    .AddQueryParam(nameof(a), a)
                                    .AddQueryParam(nameof(b), b)
                                    .AddQueryParam(nameof(ids), ids)
                                    .AddQueryParam(nameof(d), d, null)
                                    .AddQueryParam(nameof(e), e, true)
                                    .AddQueryParam(nameof(f), f, null)
                                    .AddQueryParam(nameof(g), g, "Cake")
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("DELETE"), uri);
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParamsAndComplexUdtAsync(Dibix.Sdk.Tests.DomainModel.AnotherInputContract body, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), "Tests/GenericEndpoint");
                if (_httpAuthorizationProvider.GetValue("DibixClientId") != null)
                    requestMessage.Headers.Add("DBXNS-ClientId", _httpAuthorizationProvider.GetValue("DibixClientId"));
                else if (_httpAuthorizationProvider.GetValue("DibixBearer") != null)
                    requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                else
                    throw new InvalidOperationException("None of the security scheme requirements were met:\r\n- DibixClientId\r\n- DibixBearer");
                requestMessage.Content = new ObjectContent<Dibix.Sdk.Tests.DomainModel.AnotherInputContract>(body, Formatter);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParamsAnonymousAsync(string? password, string a, string b, IEnumerable<int> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                Uri uri = UriBuilder.Create($"Tests/GenericEndpoint/{password}/User", UriKind.Relative)
                                    .AddQueryParam(nameof(a), a)
                                    .AddQueryParam(nameof(b), b)
                                    .AddQueryParam(nameof(ids), ids)
                                    .AddQueryParam(nameof(d), d, null)
                                    .AddQueryParam(nameof(e), e, true)
                                    .AddQueryParam(nameof(f), f, null)
                                    .AddQueryParam(nameof(g), g, "Cake")
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), uri);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponse<System.IO.Stream>> FileResultAsync(int id, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), $"Tests/GenericEndpoint/{id}");
                if (_httpAuthorizationProvider.GetValue("Bearer") != null)
                    requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("Bearer")}");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                System.IO.Stream responseContent = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new HttpResponse<System.IO.Stream>(responseMessage, responseContent);
            }
        }

        public async Task<HttpResponseMessage> FileUploadAsync(System.IO.Stream body, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("PUT"), "Tests/GenericEndpoint");
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                requestMessage.Content = new StreamContent(body);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponse<IReadOnlyList<Dibix.Sdk.Tests.DomainModel.GenericContract>>> MultiConcreteResultAsync(CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "Tests/GenericEndpoint");
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                IReadOnlyList<Dibix.Sdk.Tests.DomainModel.GenericContract> responseContent = await responseMessage.Content.ReadAsAsync<IReadOnlyList<Dibix.Sdk.Tests.DomainModel.GenericContract>>(MediaTypeFormattersFactory.Create(_httpClientOptions, client), cancellationToken).ConfigureAwait(false);
                return new HttpResponse<IReadOnlyList<Dibix.Sdk.Tests.DomainModel.GenericContract>>(responseMessage, responseContent);
            }
        }

        public async Task<HttpResponse<string>> ReflectionTargetAsync(int id, string? name = null, int age = 18, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                Uri uri = UriBuilder.Create($"Tests/GenericEndpoint/Reflection/{id}", UriKind.Relative)
                                    .AddQueryParam(nameof(age), age, 18)
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), uri);
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                if (name != null)
                    requestMessage.Headers.Add("name", name);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                string responseContent = await responseMessage.Content.ReadAsAsync<string>(MediaTypeFormattersFactory.Create(_httpClientOptions, client), cancellationToken).ConfigureAwait(false);
                return new HttpResponse<string>(responseMessage, responseContent);
            }
        }

        public async Task<HttpResponse<Dibix.Sdk.Tests.DomainModel.GenericContract>> SingleConrecteResultWithArrayParamAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                Uri uri = UriBuilder.Create("Tests/GenericEndpoint/Array", UriKind.Relative)
                                    .AddQueryParam(nameof(ids), ids)
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), uri);
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                Dibix.Sdk.Tests.DomainModel.GenericContract responseContent = await responseMessage.Content.ReadAsAsync<Dibix.Sdk.Tests.DomainModel.GenericContract>(MediaTypeFormattersFactory.Create(_httpClientOptions, client), cancellationToken).ConfigureAwait(false);
                return new HttpResponse<Dibix.Sdk.Tests.DomainModel.GenericContract>(responseMessage, responseContent);
            }
        }

        public async Task<HttpResponse<Dibix.Sdk.Tests.DomainModel.GenericContract>> SingleConrecteResultWithParamsAsync(int id, string name, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = _httpClientFactory.CreateClient(_httpClientName))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), $"Tests/GenericEndpoint/User/{id}/{name}");
                requestMessage.Headers.Add("Authorization", $"Bearer {_httpAuthorizationProvider.GetValue("DibixBearer")}");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                Dibix.Sdk.Tests.DomainModel.GenericContract responseContent = await responseMessage.Content.ReadAsAsync<Dibix.Sdk.Tests.DomainModel.GenericContract>(MediaTypeFormattersFactory.Create(_httpClientOptions, client), cancellationToken).ConfigureAwait(false);
                return new HttpResponse<Dibix.Sdk.Tests.DomainModel.GenericContract>(responseMessage, responseContent);
            }
        }
    }
}
#endregion