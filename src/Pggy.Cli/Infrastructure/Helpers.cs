using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Infrastructure
{
    public static class Helpers
    {
        public static string Humanize(this TimeSpan ts)
        {
            string result = ts.TotalMilliseconds < 999 ?
                $"{ts.TotalMilliseconds} ms" :
                string.Format("{0:%h} hours {0:%m} minutes {0:%s} seconds", ts);

            return result;
        }
    }
}
