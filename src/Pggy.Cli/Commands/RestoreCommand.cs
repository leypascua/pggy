using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pggy.Cli.Infrastructure;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Commands
{
    public static class RestoreCommand
    {
        public static ConsoleAppBuilder AddRestoreCommand(this ConsoleAppBuilder builder)
        {
            builder.AddCommand(sp =>
            {
                // pggy restore --dump "C:/dumpfile.sql.gz" --target hp_muppet_programsetupdb_live
                var restore = new Command("restore", "Restore a (g)zipped archive plain text dump from pg_dump to a destination database");

                var dumpOpt = new Option<string>("--dump", "[dbdump.sql.gz] A (g)zipped archive produced by pg_dump using the -Fp option.");
                var targetOpt = new Option<string>("--target", "A Npgsql connection string (or name of connection in the ConnectionStrings section of the config file) of the target db");

                restore.AddOption(dumpOpt);
                restore.AddOption(targetOpt);

                restore.SetHandler(async (context) =>
                {
                    var inputs = new Inputs
                    {
                        DumpFile = context.ParseResult.GetValueForOption(dumpOpt),
                        TargetDb = context.ParseResult.GetValueForOption(targetOpt)
                    };

                    context.ExitCode = await Execute(inputs, sp.GetService<IConfiguration>(), context.Console);
                });

                return restore;
            });

            return builder;
        }

        private static async Task<int> Execute(Inputs inputs, IConfiguration config, IConsole console)
        {
            var csb = config.GetNpgsqlConnectionString(inputs.TargetDb) ?? new Npgsql.NpgsqlConnectionStringBuilder(inputs.TargetDb);
            if (csb == null)
            {
                console.Error.WriteLine($"Unable to resolve a valid connection string.");
                return ExitCodes.Error;
            }

            var dbDump = new FileInfo(inputs.DumpFile);
            if (!dbDump.Exists)
            {
                console.Error.WriteLine($"Unable to load db dump file: [{dbDump.Name}]");
                return ExitCodes.Error;
            }

            bool isDatabaseDropped = await DropDatabase(csb, console);
            if (!isDatabaseDropped)
            {
                return ExitCodes.Success;
            }

            return ExitCodes.Success;
        }

        private static async Task<bool> DropDatabase(NpgsqlConnectionStringBuilder csb, IConsole console)
        {
            var connStr = new NpgsqlConnectionStringBuilder(csb.ToString());

            connStr.Timeout = 5;
            connStr.Pooling = false;
            connStr.Database = "postgres";

            using (var conn = new NpgsqlConnection(connStr.ToString()))
            {
                conn.Open();

                console.WriteLine($"*** WARNING! This destructive command will drop the database [{csb.Database}] on server [{csb.Host}] which cannot be reversed. Press [CTRL+C] now to abort...");

                await Task.Delay(TimeSpan.FromSeconds(5));

                // kill processes
                var killCmd = conn.CreateCommand();
                killCmd.CommandText = typeof(RestoreCommand).Assembly.GetManifestResourceString("Pggy.Cli.Resources.killprocesses.command.sql");
                killCmd.Parameters.Add(new() { ParameterName = "dbName", Value = csb.Database });
                await killCmd.ExecuteNonQueryAsync();

                // drop database
                var dropCmd = conn.CreateCommand();
                killCmd.CommandText = $"DROP DATABASE {csb.Database};";
                await killCmd.ExecuteNonQueryAsync();

                return true;
            }
        }

        public class Inputs
        {
            public string DumpFile { get; set; }
            public string TargetDb { get; set; }
        }
    }
}
