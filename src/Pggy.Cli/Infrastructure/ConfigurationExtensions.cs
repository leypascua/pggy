using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Infrastructure
{
    public static class ConfigurationExtensions
    {
        public static T BindSectionAs<T>(this IConfiguration configuration, string sectionName) where T : class, new()
        {
            T conf = new T();

            var section = configuration.GetRequiredSection(sectionName);
            section.Bind(conf);

            return conf;
        }

        public static NpgsqlConnectionStringBuilder GetNpgsqlConnectionString(this IConfiguration config, string name)
        {
            string result = config.GetConnectionString(name);

            return result == null ? null : new NpgsqlConnectionStringBuilder(result);
        }
    }
}
