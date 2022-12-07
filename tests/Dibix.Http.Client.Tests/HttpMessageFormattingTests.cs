using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Http.Client.Tests
{
    [TestClass]
    public sealed class HttpMessageFormattingTests
    {
        [TestMethod]
        public async Task HttpException_GetFormattedText_WithMaskedSecret()
        {
            MethodInfo createMethod = typeof(HttpException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(HttpRequestMessage), typeof(HttpResponseMessage) });
            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "Secret!");
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password")
              , new KeyValuePair<string, string>("client_id", "client_id")
              , new KeyValuePair<string, string>("username", "username")
              , new KeyValuePair<string, string>("password", "password")
            });
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(typeof(HttpMessageFormattingTests)!.FullName!, "1.0"));

            HttpResponseMessage response = request.CreateResponse();
            response.Headers.Date = new DateTimeOffset(2022, 8, 3, 15, 30, 20, TimeSpan.Zero);
            response.Content = new ObjectContent<string>("Cake", new JsonMediaTypeFormatter());

            HttpException httpException = await ((Task<HttpException>)createMethod.Invoke(null, new object[] { request, response })!).ConfigureAwait(false);

            const string expected = @"Request
-------
GET  HTTP/1.1
Accept: application/json, text/json
Authorization: Bearer Secre...
User-Agent: Dibix.Http.Client.Tests.HttpMessageFormattingTests/1.0
Content-Type: application/x-www-form-urlencoded
Content-Length: 75

grant_type=password&client_id=client_id&username=username&password=*****

Response
--------
HTTP/1.1 200 OK
Date: Wed, 03 Aug 2022 15:30:20 GMT
Content-Type: application/json; charset=utf-8

""Cake""";
            string actual = httpException.GetFormattedText();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task HttpException_GetFormattedText_WithNoMaskedSecret()
        {
            MethodInfo createMethod = typeof(HttpException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(HttpRequestMessage), typeof(HttpResponseMessage) });
            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "Secret!");
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password")
              , new KeyValuePair<string, string>("client_id", "client_id")
              , new KeyValuePair<string, string>("username", "username")
              , new KeyValuePair<string, string>("password", "password")
            });

            HttpResponseMessage response = request.CreateResponse();
            response.Content = new ByteArrayContent(Array.Empty<byte>());

            HttpException httpException = await ((Task<HttpException>)createMethod.Invoke(null, new object[] { request, response })!).ConfigureAwait(false);

            const string expected = @"Request
-------
GET  HTTP/1.1
Authorization: Bearer Secret!
Content-Type: application/x-www-form-urlencoded
Content-Length: 75

grant_type=password&client_id=client_id&username=username&password=password

Response
--------
HTTP/1.1 200 OK
Content-Length: 0";
            string actual = httpException.GetFormattedText(maskSensitiveData: false);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task HttpException_GetFormattedText_WithUnmaskedSecret()
        {
            MethodInfo createMethod = typeof(HttpException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(HttpRequestMessage), typeof(HttpResponseMessage) });
            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "Okay");

            HttpResponseMessage response = request.CreateResponse();
            response.Content = new ByteArrayContent(Array.Empty<byte>());

            HttpException httpException = await ((Task<HttpException>)createMethod.Invoke(null, new object[] { request, response })!).ConfigureAwait(false);

            const string expected = @"Request
-------
GET  HTTP/1.1
Authorization: Bearer Okay

Response
--------
HTTP/1.1 200 OK
Content-Length: 0";
            string actual = httpException.GetFormattedText();
            Assert.AreEqual(expected, actual);
        }
    }
}