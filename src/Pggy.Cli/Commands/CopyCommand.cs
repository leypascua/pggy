using Microsoft.Extensions.Configuration;
using Pggy.Cli.Infrastructure;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine.IO;

namespace Pggy.Cli.Commands
{
    public static class CopyCommand
    {
        public static ConsoleAppBuilder AddCopyCommand(this ConsoleAppBuilder builder)
        {
            builder.AddCommand(sp =>
            {
                // pggy restore --dump "C:/dumpfile.sql.gz" --target hp_muppet_programsetupdb_live
                var restore = new Command("copy", "Copy a source database into a destination database");

                var sourceOpt = new Option<string>("--source", "A Npgsql connection string (or name of connection in the ConnectionStrings section of the config file) of the source db");
                var destOpt = new Option<string>("--dest", "A Npgsql connection string (or name of connection in the ConnectionStrings section of the config file) of the destination db");
                var forceOpt = new Option<bool>("--force", "Perform the command right away, without delay");

                restore.AddOption(sourceOpt);
                restore.AddOption(destOpt);
                restore.AddOption(forceOpt);

                restore.SetHandler(async (context) =>
                {
                    var inputs = new Inputs
                    {
                        SourceDb = context.ParseResult.GetValueForOption(sourceOpt),
                        DestDb = context.ParseResult.GetValueForOption(destOpt),
                        IsForced = context.ParseResult.GetValueForOption(forceOpt)
                    };

                    context.ExitCode = await Execute(inputs, sp.GetService<IConfiguration>(), context.Console);
                });

                return restore;
            });

            return builder;
        }

        private static async Task<int> Execute(Inputs inputs, IConfiguration config, IConsole console)
        {
            // get source connection string

            // get destination connection string

            // if source and destination DBs are on the same host, use `CREATE DATABASE {dest.Database} WITH TEMPLATE {source.Database} OWNER {dest.Username};`

            // if source and destination are on different hosts, use PG_DUMP to pipe the source DB into the dest DB.

            console.Error.WriteLine("This command is not implemented.");
            return await Task.FromResult(ExitCodes.Error);
        }

        public class Inputs
        {
            public string SourceDb { get; set; }
            public string DestDb { get; set; }
            public bool IsForced { get; set; }
        }
    }
}
