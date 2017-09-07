using System.IO;
using System.IO.Compression;

namespace WebApi.OutputCache.Redis.Extensions
{
    public static class ByteArrayExtensions
    {
        public static byte[] Compress(
            this byte[] target, 
            CompressionLevel compressionLevel = CompressionLevel.Fastest
        )
        {
            if (target == null)
            {
                return null;
            }

            using (MemoryStream input = new MemoryStream(target))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (GZipStream compressionStream = new GZipStream(output, compressionLevel))
                    {
                        input.CopyTo(compressionStream);
                    }

                    return output.ToArray();
                }
            }
        }

        public static byte[] Decompress(this byte[] target)
        {
            if (target == null)
            {
                return null;
            }

            using (MemoryStream input = new MemoryStream(target))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (GZipStream decompressionStream = new GZipStream(input, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(output);
                    }

                    return output.ToArray();
                }
            }
        }
    }
}
