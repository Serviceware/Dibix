using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Dibix.Http.Server.AspNet
{
    public static class HttpRequestExecutionExceptionExtensions
    {
        public static readonly IDictionary<int, (string Type, string Title)> Defaults = new Dictionary<int, (string Type, string Title)>
        {
            [400] = ("https://tools.ietf.org/html/rfc9110#section-15.5.1",  "Bad Request"),
            [401] = ("https://tools.ietf.org/html/rfc9110#section-15.5.2",  "Unauthorized"),
            [403] = ("https://tools.ietf.org/html/rfc9110#section-15.5.4",  "Forbidden"),
            [404] = ("https://tools.ietf.org/html/rfc9110#section-15.5.5",  "Not Found"),
            [405] = ("https://tools.ietf.org/html/rfc9110#section-15.5.6",  "Method Not Allowed"),
            [406] = ("https://tools.ietf.org/html/rfc9110#section-15.5.7",  "Not Acceptable"),
            [407] = ("https://tools.ietf.org/html/rfc9110#section-15.5.8",  "Proxy Authentication Required"),
            [408] = ("https://tools.ietf.org/html/rfc9110#section-15.5.9",  "Request Timeout"),
            [409] = ("https://tools.ietf.org/html/rfc9110#section-15.5.10", "Conflict"),
            [410] = ("https://tools.ietf.org/html/rfc9110#section-15.5.11", "Gone"),
            [411] = ("https://tools.ietf.org/html/rfc9110#section-15.5.12", "Length Required"),
            [412] = ("https://tools.ietf.org/html/rfc9110#section-15.5.13", "Precondition Failed"),
            [413] = ("https://tools.ietf.org/html/rfc9110#section-15.5.14", "Content Too Large"),
            [414] = ("https://tools.ietf.org/html/rfc9110#section-15.5.15", "URI Too Long"),
            [415] = ("https://tools.ietf.org/html/rfc9110#section-15.5.16", "Unsupported Media Type"),
            [416] = ("https://tools.ietf.org/html/rfc9110#section-15.5.17", "Range Not Satisfiable"),
            [417] = ("https://tools.ietf.org/html/rfc9110#section-15.5.18", "Expectation Failed"),
            [421] = ("https://tools.ietf.org/html/rfc9110#section-15.5.20", "Misdirected Request"),
            [422] = ("https://tools.ietf.org/html/rfc4918#section-11.2",    "Unprocessable Entity"),
            [426] = ("https://tools.ietf.org/html/rfc9110#section-15.5.22", "Upgrade Required"),
            [500] = ("https://tools.ietf.org/html/rfc9110#section-15.6.1",  "An error occurred while processing your request."),
            [501] = ("https://tools.ietf.org/html/rfc9110#section-15.6.2",  "Not Implemented"),
            [502] = ("https://tools.ietf.org/html/rfc9110#section-15.6.3",  "Bad Gateway"),
            [503] = ("https://tools.ietf.org/html/rfc9110#section-15.6.4",  "Service Unavailable"),
            [504] = ("https://tools.ietf.org/html/rfc9110#section-15.6.5",  "Gateway Timeout"),
            [505] = ("https://tools.ietf.org/html/rfc9110#section-15.6.6",  "HTTP Version Not Supported")
        };

        public static HttpResponseMessage CreateResponse(this HttpRequestExecutionException exception, HttpRequestMessage request)
        {
            HttpResponseMessage response = request.CreateResponse(exception.StatusCode);
            int statusCode = (int)exception.StatusCode;
            _ = Defaults.TryGetValue(statusCode, out (string Type, string Title) defaults);

            JObject problemDetails = new JObject
            {
                ["type"] = defaults.Type,
                ["title"] = defaults.Title,
                ["status"] = statusCode
            };

            if (exception.IsClientError)
            {
                problemDetails.Add("code", exception.ErrorCode);
                problemDetails.Add("detail", exception.ErrorMessage);
            }

            const string contentType = "application/problem+json";
            response.Content = new StringContent(problemDetails.ToString(), Encoding.UTF8, contentType);
            return response;
        }
    }
}