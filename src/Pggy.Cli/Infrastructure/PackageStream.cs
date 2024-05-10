using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Infrastructure
{
    public static class PackageStream
    {
        public static Stream Open(FileStream file)
        {
            string extension = Path.GetExtension(file.Name);

            switch (extension)
            {
                case ".gz":
                    return new GZipStream(file, CompressionMode.Decompress);

                case ".br":
                    return new BrotliStream(file, CompressionMode.Decompress);

                case ".sql":
                    return file;

                default:
                    throw new NotSupportedException($"Unsupported file type detected: [{file.Name}]");
            }
        }
    }
}
