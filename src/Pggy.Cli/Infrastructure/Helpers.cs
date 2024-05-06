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
            if (ts.TotalSeconds < 1)
            {
                return $"{ts.TotalMilliseconds} ms";
            }

            if (ts.TotalSeconds < 120)
            {
                return $"{ts.TotalSeconds} seconds";
            }

            if (ts.TotalMinutes < 120)
            {
                return $"{ts.TotalMinutes} minutes";
            }

            return string.Format("{0:%h} hours {0:%m} minutes {0:%s} seconds", ts);
        }
    }
}
