using System;

namespace AspNetCore.CacheOutput
{
    public class ResponseAdditionalData
    {
        public int StatusCode { get; set; }

        public string ContentType { get; set; }

        public string Etag { get; set; }

        public DateTimeOffset LastModified { get; set; }
    }
}
