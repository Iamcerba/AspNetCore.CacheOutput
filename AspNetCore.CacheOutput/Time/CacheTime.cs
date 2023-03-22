using System;

namespace AspNetCore.CacheOutput.Time
{
    public class CacheTime
    {
        public TimeSpan? ClientTimeSpan { get; set; }

        public TimeSpan? SharedTimeSpan { get; set; }

        public DateTimeOffset AbsoluteExpiration { get; set; }
    }
}
