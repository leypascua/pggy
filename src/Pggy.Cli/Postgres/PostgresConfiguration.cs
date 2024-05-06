using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Postgres
{
    public class PostgresConfiguration
    {
        const string PG_DUMP = "pg_dump.exe";
        const string PSQL = "psql.exe";

        public string bin { get; set; }

        public string pg_dump()
        {
            string binPath = string.IsNullOrEmpty(bin) ? "%PGHOME%" : (bin ?? string.Empty).Trim();

            return Path.Combine(binPath, PG_DUMP);
        }

        public string psql()
        {
            string binPath = string.IsNullOrEmpty(bin) ? "%PGHOME%" : (bin ?? string.Empty).Trim();

            return Path.Combine(binPath, PSQL);
        }
    }
}
