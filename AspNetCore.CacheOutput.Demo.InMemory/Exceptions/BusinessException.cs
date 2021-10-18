using System;
using System.Runtime.Serialization;

namespace AspNetCore.CacheOutput.Demo.InMemory.Exceptions
{
    [Serializable]
    public class BusinessException : Exception {
        public BusinessException()
        {
        }

        public BusinessException(string message) : base(message)
        {
        }

        public BusinessException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BusinessException(
            SerializationInfo info,
            StreamingContext context
        ) : base(info, context)
        {
        }
    }
}
