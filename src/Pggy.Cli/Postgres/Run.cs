using Microsoft.Extensions.Configuration;
using Pggy.Cli.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Postgres
{
    using Npgsql;
    using System.IO;

    public static class Run
    {
        public static ChildProcessBuilder PgDump(string sourceDb, string destPath, IConfiguration config)
        {
            var postgres = config.BindSectionAs<PostgresConfiguration>("Postgres");

            string connectionString = config.GetConnectionString(sourceDb) ?? sourceDb;
            var csb = new NpgsqlConnectionStringBuilder(connectionString);

            string dumpDir = GetValidDestinationPath(destPath);
            string filename = $"{csb.Database}.{DateTime.UtcNow.ToString("yyyyMMddThhmm")}.sql.zip";
            string finalDumpPath = Path.Combine(dumpDir, filename);

            var builder = new ChildProcessBuilder(postgres.pg_dump())
                .Option("-h", csb.Host)
                .Option("-p", csb.Port)
                .Option("-U", csb.Username)
                .Option("--no-password")
                .Option("-d", csb.Database)
                .Option("-Fp")
                .Option("-Z", 6)
                .Option("-f", $"\"{finalDumpPath}\"");

            bool isPasswordSet = false;

            if (!string.IsNullOrEmpty(csb.Password) && string.IsNullOrEmpty(csb.Passfile))
            {
                builder.SetVar("PGPASSWORD", csb.Password);
                isPasswordSet = true;
            }                

            if (!isPasswordSet && !string.IsNullOrEmpty(csb.Passfile) && File.Exists(csb.Passfile))
            {
                builder.SetVar("PGPASSFILE", csb.Passfile);
            }

            return builder;
        }

        private static string GetValidDestinationPath(string path)
        {
            string finalPath = Path.IsPathRooted(path) ?
                path :
                Path.Combine(Environment.CurrentDirectory, path);

            if (!Directory.Exists(finalPath))
            {
                Directory.CreateDirectory(finalPath);
            }

            return finalPath;
        }
    }
}
