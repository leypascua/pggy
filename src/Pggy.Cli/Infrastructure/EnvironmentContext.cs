using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Infrastructure
{
    public class EnvironmentContext
    {
        public static string Name
        {
            get
            {
                string result = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                    Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT") ??
                    Environments.Development;

                return result;
            }
        }
    }
}
