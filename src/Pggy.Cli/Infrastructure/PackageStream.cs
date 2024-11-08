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
        const string ZIP = ".zip";

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
                case ZIP:
                    return FromZipFile(file);

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

        public static Stream FromZipFile(FileStream file)
        {
            var zipArchive = new ZipArchive(file, ZipArchiveMode.Read);

            // Validate archive contains exactly one entry
            if (zipArchive.Entries.Count != 1)
            {
                throw new NotSupportedException($"Unable to restore database from [{file.Name}]. Ensure that it only contains the plain text SQL result of pg_dump.");
            }

            ZipArchiveEntry entry = zipArchive.Entries[0];

            // Validate entry has .sql extension
            if (!entry.Name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("ZIP archive entry must have .sql extension");
            }

            // Validate entry name matches zip file name (with .sql extension)
            // Get the expected SQL filename by replacing .zip extension with .sql
            string expectedSqlFileName = Path.ChangeExtension(Path.GetFileName(file.Name), SQL);

            if (!string.Equals(entry.Name, expectedSqlFileName, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    "ZIP entry name must match ZIP file name with .sql extension"
                );
            }

            // Open and return the stream
            // Note: The caller is responsible for disposing this stream
            return entry.Open();
        }
    }
}
