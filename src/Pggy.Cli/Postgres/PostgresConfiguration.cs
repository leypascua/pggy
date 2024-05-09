using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Postgres
{
    public class PostgresConfiguration
    {
        private readonly static string PG_DUMP = GetExe("pg_dump");

        private readonly static string PSQL = GetExe("psql");

        public string bin { get; set; }

        public string pg_dump()
        {
            return ExePath(PG_DUMP);
        }

        public string psql()
        {
            return ExePath(PSQL);
        }

        private string PgHome()
        {
            return ResolvePgHomePathFrom(bin, Environment.GetEnvironmentVariable("PGHOME"));
        }

        private string ExePath(string command)
        {
            string targetPath = Path.Combine(PgHome(), command); 

            if (!Path.Exists(targetPath))
            {
                throw new FileNotFoundException($"Unable to locate executable: '{command}'");
            }

            return targetPath;
        }

        private static string GetExe(string input)
        {
            string fileExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
            return input + fileExtension;
        }

        private static string ResolvePgHomePathFrom(params string[] paths)
        {
            foreach (string path in paths)
            {
                if (!string.IsNullOrEmpty(path) && Path.Exists(path))
                {
                    return path;
                }
            }

            throw new InvalidOperationException("Unable to locate Postgres binaries. Make sure the environment variable PGHOME is set correctly.");
        }
    }
}
