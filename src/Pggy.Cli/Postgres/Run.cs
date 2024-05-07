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
        public static ChildProcessBuilder PgDump(NpgsqlConnectionStringBuilder csb, IConfiguration config)
        {
            var postgres = config.BindSectionAs<PostgresConfiguration>("Postgres");
            
            var builder = new ChildProcessBuilder(postgres.pg_dump())
                .Option("-h", csb.Host)
                .Option("-p", csb.Port)
                .Option("-U", csb.Username)
                .Option("--no-password")
                .Option("-d", csb.Database)
                .Option("-Fp");

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
    }
}
