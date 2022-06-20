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
        public string G { get; set; }

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

    public enum Role : int
    {
        None,
        User,
        Admin
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
            this.Ids = new Collection<Dibix.Sdk.Tests.DomainModel.Entry>();
        }
    }
}
#endregion

#region Interfaces
namespace Dibix.Sdk.Tests.Client
{
    public interface IGenericEndpointService : IHttpService
    {
        Task<HttpResponse<ICollection<Dibix.Sdk.Tests.DomainModel.GenericContract>>> MultiConcreteResultAsync(CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParams1Async(string password, string userAgent, IEnumerable<object> ids, string? acceptLanguage = null, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParamsAnonymousAsync(string password, string a, string b, System.Guid? c, IEnumerable<object> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default);
        Task<HttpResponse<Dibix.Sdk.Tests.DomainModel.GenericContract>> SingleConrecteResultWithParamsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<HttpResponse<System.IO.Stream>> FileResultAsync(int id, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> FileUploadAsync(System.IO.Stream body, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParams2Async(Dibix.Sdk.Tests.DomainModel.InputContract body, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParams3Async(Dibix.Sdk.Tests.DomainModel.InputContract body, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> EmptyWithParams4Async(Dibix.Sdk.Tests.DomainModel.AnotherInputContract body, CancellationToken cancellationToken = default);
    }
}
#endregion

#region Implementation
namespace Dibix.Sdk.Tests.Client
{
    [HttpService(typeof(IGenericEndpointService))]
    public sealed class GenericEndpointService : IGenericEndpointService
    {
        private static readonly Uri BaseAddress = new Uri("https://localhost/api/");
        private static readonly MediaTypeFormatter Formatter = new JsonMediaTypeFormatter();
        private readonly string _httpClientName;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpAuthorizationProvider _httpAuthorizationProvider;

        public GenericEndpointService(IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider httpAuthorizationProvider) : this(httpClientFactory, httpAuthorizationProvider, DefaultHttpClientFactory.DefaultClientName) { }
        public GenericEndpointService(IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider httpAuthorizationProvider, string httpClientName)
        {
            this._httpClientFactory = httpClientFactory;
            this._httpAuthorizationProvider = httpAuthorizationProvider;
            this._httpClientName = httpClientName;
        }

        public async Task<HttpResponse<ICollection<Dibix.Sdk.Tests.DomainModel.GenericContract>>> MultiConcreteResultAsync(CancellationToken cancellationToken = default)
        {
            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName, BaseAddress))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "Tests/GenericEndpoint");
                requestMessage.Headers.Add("HLNS-SIT", this._httpAuthorizationProvider.GetValue("HLNS-SIT"));
                requestMessage.Headers.Add("HLNS-ClientId", this._httpAuthorizationProvider.GetValue("HLNS-ClientId"));
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                ICollection<Dibix.Sdk.Tests.DomainModel.GenericContract> responseContent = await responseMessage.Content.ReadAsAsync<ICollection<Dibix.Sdk.Tests.DomainModel.GenericContract>>(MediaTypeFormattersFactory.Create(client), cancellationToken).ConfigureAwait(false);
                return new HttpResponse<ICollection<Dibix.Sdk.Tests.DomainModel.GenericContract>>(responseMessage, responseContent);
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParams1Async(string password, string userAgent, IEnumerable<object> ids, string? acceptLanguage = null, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName, BaseAddress))
            {
                Uri uri = UriBuilder.Create($"Tests/GenericEndpoint/{password}/Fixed", UriKind.Relative)
                                    .AddQueryParam(nameof(ids), ids)
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), uri);
                requestMessage.Headers.Add("HLNS-SIT", this._httpAuthorizationProvider.GetValue("HLNS-SIT"));
                requestMessage.Headers.Add("HLNS-ClientId", this._httpAuthorizationProvider.GetValue("HLNS-ClientId"));
                requestMessage.Headers.Add("User-Agent", userAgent);
                if (acceptLanguage != null)
                    requestMessage.Headers.Add("Accept-Language", acceptLanguage);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParamsAnonymousAsync(string password, string a, string b, System.Guid? c, IEnumerable<object> ids, string? d = null, bool e = true, Dibix.Sdk.Tests.DomainModel.Direction? f = null, string? g = "Cake", CancellationToken cancellationToken = default)
        {
            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName, BaseAddress))
            {
                Uri uri = UriBuilder.Create($"Tests/GenericEndpoint/{password}/User", UriKind.Relative)
                                    .AddQueryParam(nameof(a), a)
                                    .AddQueryParam(nameof(b), b)
                                    .AddQueryParam(nameof(c), c)
                                    .AddQueryParam(nameof(ids), ids)
                                    .AddQueryParam(nameof(d), d)
                                    .AddQueryParam(nameof(e), e)
                                    .AddQueryParam(nameof(f), f)
                                    .AddQueryParam(nameof(g), g)
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), uri);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponse<Dibix.Sdk.Tests.DomainModel.GenericContract>> SingleConrecteResultWithParamsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName, BaseAddress))
            {
                Uri uri = UriBuilder.Create("Tests/GenericEndpoint/Array", UriKind.Relative)
                                    .AddQueryParam(nameof(ids), ids)
                                    .Build();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), uri);
                requestMessage.Headers.Add("HLNS-SIT", this._httpAuthorizationProvider.GetValue("HLNS-SIT"));
                requestMessage.Headers.Add("HLNS-ClientId", this._httpAuthorizationProvider.GetValue("HLNS-ClientId"));
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                Dibix.Sdk.Tests.DomainModel.GenericContract responseContent = await responseMessage.Content.ReadAsAsync<Dibix.Sdk.Tests.DomainModel.GenericContract>(MediaTypeFormattersFactory.Create(client), cancellationToken).ConfigureAwait(false);
                return new HttpResponse<Dibix.Sdk.Tests.DomainModel.GenericContract>(responseMessage, responseContent);
            }
        }

        public async Task<HttpResponse<System.IO.Stream>> FileResultAsync(int id, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName, BaseAddress))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("GET"), $"Tests/GenericEndpoint/{id}");
                if (this._httpAuthorizationProvider.GetValue("Bearer") != null)
                    requestMessage.Headers.Add("Authorization", $"Bearer {this._httpAuthorizationProvider.GetValue("Bearer")}");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                System.IO.Stream responseContent = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new HttpResponse<System.IO.Stream>(responseMessage, responseContent);
            }
        }

        public async Task<HttpResponseMessage> FileUploadAsync(System.IO.Stream body, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName, BaseAddress))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("PUT"), "Tests/GenericEndpoint");
                requestMessage.Headers.Add("HLNS-SIT", this._httpAuthorizationProvider.GetValue("HLNS-SIT"));
                requestMessage.Headers.Add("HLNS-ClientId", this._httpAuthorizationProvider.GetValue("HLNS-ClientId"));
                requestMessage.Content = new StreamContent(body);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParams2Async(Dibix.Sdk.Tests.DomainModel.InputContract body, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName, BaseAddress))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("POST"), "Tests/GenericEndpoint");
                requestMessage.Headers.Add("HLNS-SIT", this._httpAuthorizationProvider.GetValue("HLNS-SIT"));
                requestMessage.Headers.Add("HLNS-ClientId", this._httpAuthorizationProvider.GetValue("HLNS-ClientId"));
                requestMessage.Content = new ObjectContent<Dibix.Sdk.Tests.DomainModel.InputContract>(body, Formatter);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParams3Async(Dibix.Sdk.Tests.DomainModel.InputContract body, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName, BaseAddress))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), "Tests/GenericEndpoint");
                requestMessage.Headers.Add("HLNS-SIT", this._httpAuthorizationProvider.GetValue("HLNS-SIT"));
                requestMessage.Headers.Add("HLNS-ClientId", this._httpAuthorizationProvider.GetValue("HLNS-ClientId"));
                requestMessage.Content = new ObjectContent<Dibix.Sdk.Tests.DomainModel.InputContract>(body, Formatter);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }

        public async Task<HttpResponseMessage> EmptyWithParams4Async(Dibix.Sdk.Tests.DomainModel.AnotherInputContract body, CancellationToken cancellationToken = default)
        {
            using (HttpClient client = this._httpClientFactory.CreateClient(this._httpClientName, BaseAddress))
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("DELETE"), "Tests/GenericEndpoint");
                requestMessage.Headers.Add("HLNS-SIT", this._httpAuthorizationProvider.GetValue("HLNS-SIT"));
                requestMessage.Headers.Add("HLNS-ClientId", this._httpAuthorizationProvider.GetValue("HLNS-ClientId"));
                requestMessage.Content = new ObjectContent<Dibix.Sdk.Tests.DomainModel.AnotherInputContract>(body, Formatter);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return responseMessage;
            }
        }
    }
}
#endregion