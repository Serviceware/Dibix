using System;
using System.Net;

namespace Dibix.Http
{
    public static class DatabaseAccessorExtensions
    {
        public static HttpFileResponse QueryFile(this IDatabaseAccessor databaseAccessor, string commandText, IParametersVisitor parameters)
        {
            FileEntity result = databaseAccessor.QuerySingleOrDefault<FileEntity>(commandText, parameters);
            if (String.IsNullOrEmpty(result.Type))
                throw new InvalidOperationException("No type was returned for file query");

            return new HttpFileResponse(HttpStatusCode.OK, MimeTypes.GetMimeType(result.Type), result.Data, true);
        }

        private class FileEntity
        {
            public string Type { get; set; }
            public byte[] Data { get; set; }
        }
    }
}