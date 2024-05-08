using Pggy.Cli.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Tests.Unit
{
    public class AssemblyExtensionsTests
    {
        [Fact]
        public void GetResourceString_WhenGivenWrongeName_ReturnsValue()
        {
            string result = typeof(AssemblyExtensions).Assembly.GetManifestResourceString("Pggy.Cli.Resources.killprocesses.command.sql");

            Assert.NotEmpty(result);
        }

        [Fact]
        public void GetResourceString_WhenGivenWrongeName_ReturnsEmptyString()
        {
            string result = typeof(AssemblyExtensions).Assembly.GetManifestResourceString("i.am.invalid");

            Assert.Empty(result);
        }
    }
}
