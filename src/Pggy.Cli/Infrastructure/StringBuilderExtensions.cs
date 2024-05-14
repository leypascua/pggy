using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Pggy.Cli.Infrastructure
{
    public static class StringBuilderExtensions
    {
        public static bool EndsWith(this StringBuilder input, string expected)
        {
            // If the expected length is greater than the input length, it can't be a match
            if (expected.Length > input.Length) return false;

            // Check if the end of input matches expected
            for (int i = 0; i < expected.Length; i++)
            {
                if (input[input.Length - expected.Length + i] != expected[i])
                    return false;
            }

            return true;
        }
    }
}
