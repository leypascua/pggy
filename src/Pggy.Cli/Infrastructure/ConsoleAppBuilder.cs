using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pggy.Cli.Infrastructure
{
    public class ConsoleAppBuilder
    {
        private readonly IList<Func<IServiceProvider, Command>> _commandFactories = new List<Func<IServiceProvider, Command>>();
        private readonly string _description;

        public static ConsoleAppBuilder Create(string description)
        {
            return new ConsoleAppBuilder(description);
        }

        private ConsoleAppBuilder(string description) 
        {
            _description = description;
            this.Configuration = new ConfigurationBuilder();
            this.Services = new ServiceCollection();
        }

        public ConfigurationBuilder Configuration { get; private set; }
        public IServiceCollection Services { get; private set; }

        public ConsoleAppBuilder AddCommand(Func<IServiceProvider, Command> buildCommand)
        {
            _commandFactories.Add(buildCommand);
            return this;
        }

        public RootCommand Build()
        {
            IConfiguration config = this.Configuration.Build();
            this.Services.AddSingleton(config);

            var serviceProvider = this.Services.BuildServiceProvider();

            var root = new RootCommand(_description);

            foreach (var buildCommand in _commandFactories)
            {
                var cmd = buildCommand(serviceProvider);
                root.AddCommand(cmd);
            }

            return root;
        }
    }
}
