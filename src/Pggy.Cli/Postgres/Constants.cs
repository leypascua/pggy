using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Postgres
{
    public static class Constants
    {
        public const string DEFAULT_USER = "postgres";
        public readonly static int PGDUMP_READ_BUFFER_SIZE = 2 * 1024 * 1024;
    }
}
