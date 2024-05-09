using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Postgres
{
    public static class Helpers
    {
        public static bool IsValidConnectionString(this string input)
        {
            try
            {
                var cb = new NpgsqlConnectionStringBuilder(input);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
