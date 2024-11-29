using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionResponse
    {
        private readonly IDictionary<int, ErrorDescription> _errors = new Dictionary<int, ErrorDescription>();

        public HttpStatusCode StatusCode { get; }
        public string MediaType { get; } = HttpMediaType.Json;
        public TypeReference ResultType { get; set; }
        public string Description { get; set; }
        public ErrorDescription StatusCodeDetectionDetail { get; set; }
        public ICollection<ErrorDescription> Errors => _errors.Values;

        public ActionResponse(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }
        public ActionResponse(HttpStatusCode statusCode, TypeReference resultType) : this(statusCode)
        {
            ResultType = resultType;
        }
        public ActionResponse(HttpStatusCode statusCode, string mediaType, TypeReference resultType) : this(statusCode)
        {
            MediaType = mediaType;
            ResultType = resultType;
        }

        public void AddError(int errorCode, string errorDescription, SourceLocation sourceLocation, ILogger logger)
        {
            if (!_errors.Any())
            {
                SchemaDefinition problemDetailsSchema = BuiltInSchemaProvider.ProblemDetailsSchema;
                ResultType = new SchemaTypeReference(key: problemDetailsSchema.FullName, isNullable: false, isEnumerable: false, problemDetailsSchema.Location);
            }

            if (_errors.TryGetValue(errorCode, out ErrorDescription existingErrorDescription))
            {
                if (existingErrorDescription.Description != errorDescription)
                {
                    logger.LogError($"Ambiguous validation error code: {existingErrorDescription.ErrorCode}{(!String.IsNullOrEmpty(existingErrorDescription.Description) ? $" ({existingErrorDescription.Description})" : null)}", existingErrorDescription.Location);
                    logger.LogError($"Ambiguous validation error code: {errorCode}{(!String.IsNullOrEmpty(errorDescription) ? $" ({errorDescription})" : null)}", sourceLocation);
                }
                return;
            }
            _errors.Add(errorCode, new ErrorDescription(errorCode, errorDescription, sourceLocation));
        }
    }
}