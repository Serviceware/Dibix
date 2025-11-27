using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Http.Client.Tests
{
    [TestClass]
    public sealed class HttpExceptionTests
    {
        [TestMethod]
        public async Task HttpException_GetFormattedText_WithMaskedSecret()
        {
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
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(typeof(HttpExceptionTests)!.FullName!, "1.0"));

            HttpResponseMessage response = request.CreateResponse();
            response.Headers.Date = new DateTimeOffset(2022, 8, 3, 15, 30, 20, TimeSpan.Zero);
            response.Content = new ObjectContent<string>("Cake", new JsonMediaTypeFormatter());

            HttpException httpException = await HttpException.Create(request, response).ConfigureAwait(false);

            const string expected = @"Request
-------
GET  HTTP/1.1
Accept: application/json, text/json
Authorization: Bearer Secre...
User-Agent: Dibix.Http.Client.Tests.HttpExceptionTests/1.0
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

            HttpException httpException = await HttpException.Create(request, response).ConfigureAwait(false);

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
            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "Okay");

            HttpResponseMessage response = request.CreateResponse();
            response.Content = new ByteArrayContent(Array.Empty<byte>());

            HttpException httpException = await HttpException.Create(request, response).ConfigureAwait(false);

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

        [TestMethod]
        public async Task HttpException_WithProblemDetailsAndCode_ReturnsHttpValidationException()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotFound);
            response.Content = new ObjectContent<Dictionary<string, object>>(new Dictionary<string, object>
            {
                ["type"] = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                ["title"] = "Not Found",
                ["status"] = 404,
                ["detail"] = "Service not available",
                ["code"] = 17
            }, new JsonMediaTypeFormatter(), "application/problem+json");
            HttpException httpException = await HttpException.Create(request, response).ConfigureAwait(false);
            HttpValidationException validationException = Assert.IsInstanceOfType<HttpValidationException>(httpException);
            Assert.AreEqual(17, validationException.ErrorCode);
            Assert.AreEqual("Service not available", validationException.ErrorMessage);
        }
    }
}