using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Infrastructure
{
    public static class AssemblyExtensions
    {
        public static string GetManifestResourceString(this Assembly assembly, string resourceName)
        {
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return string.Empty;

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
