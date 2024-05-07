using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Infrastructure
{
    public static class TimeSpanExtensions
    {
        public static string Humanize(this TimeSpan ts)
        {
            string result = string.Empty; 

            if (ts.TotalMilliseconds < 1)
            {
                result = Pluralize((int)ts.TotalMilliseconds, "millisecond");
            }
            else if (ts.TotalSeconds < 120)
            {
                result = Pluralize((int)ts.TotalSeconds, "second");
            }
            else if (ts.TotalMinutes < 120)
            {
                int minutes = (int)ts.TotalMinutes;
                int seconds = ts.Seconds;

                result = $"{Pluralize(minutes, "minute")} {Pluralize(seconds, "second")}";
            }
            else
            {
                int hours = (int)ts.TotalHours;
                int minutes = ts.Minutes;
                int seconds = ts.Seconds;

                result = $"{Pluralize(hours, "hour")} {Pluralize(minutes, "minute")} {Pluralize(seconds, "second")}";
            }

            string trimmed = result.Trim();

            return trimmed.Length == 0 ? $"{ts.TotalMilliseconds} ms" : trimmed;
        }

        private static string Pluralize(int val, string unit)
        {
            if (val == 0) return string.Empty;

            return val > 1 ?
                $"{val} {unit}s" :
                $"{val} {unit}";
        }
    }
}
