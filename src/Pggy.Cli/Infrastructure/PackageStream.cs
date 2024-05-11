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
        const string GZ = ".gz";
        const string BR = ".br";
        const string SQL = ".sql";

        public static Stream CreateWith(FileStream file)
        {
            string extension = Path.GetExtension(file.Name);
            
            switch (extension)
            {
                case GZ:
                    return new GZipStream(file, CompressionLevel.SmallestSize);

                case BR:
                    return new BrotliStream(file, CompressionLevel.Optimal);

                case SQL:
                    return file;

                default:
                    throw new NotSupportedException($"Unsupported file type detected: [{extension}]");
            }
        }

        public static Stream Open(FileStream file)
        {
            string extension = Path.GetExtension(file.Name);

            switch (extension)
            {
                case GZ:
                    return new GZipStream(file, CompressionMode.Decompress);

                case BR:
                    return new BrotliStream(file, CompressionMode.Decompress);

                case SQL:
                    return file;

                default:
                    throw new NotSupportedException($"Unsupported file type detected: [{file.Name}]");
            }
        }
    }
}
