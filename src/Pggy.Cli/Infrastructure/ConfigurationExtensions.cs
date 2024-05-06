using Microsoft.Extensions.Configuration;
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
    }
}
