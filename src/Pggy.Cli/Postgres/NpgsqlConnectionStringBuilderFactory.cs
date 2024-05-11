using Microsoft.Extensions.Configuration;
using Npgsql;
using Pggy.Cli.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Postgres
{
    public class NpgsqlConnectionStringBuilderFactory
    {
        private readonly IConfiguration _config;
        
        public NpgsqlConnectionStringBuilderFactory(IConfiguration config) 
        {
            _config = config;
        }

        public NpgsqlConnectionStringBuilder CreateBuilderFrom(string input)
        {
            var result = _config.GetNpgsqlConnectionString(input);

            if (result == null)
            {
                try
                {
                    result = new NpgsqlConnectionStringBuilder(input);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            return result;
        }
    }
}
