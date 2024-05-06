using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    using Npgsql;
    using Pggy.Cli;

    public static class BackupCommand
    {
        public static ConsoleAppBuilder AddBackupCommand(this ConsoleAppBuilder builder)
        {
            builder.AddCommand(sp =>
            {
                // pggy backup --src hp_muppet_programsetupdb_live--dest "L:\PostgreSQLBackup\hp_muppet_programsetupdb_live"
                var backup = new Command("backup", "Backup a source database into a file.");

                var srcOpt = new Option<string>("--src", "A Npgsql connection string (or name of connection in the ConnectionStrings section of the config file) of the source db");
                var destOpt = new Option<string>("--dest", "The destination path of the resulting DB dump file that can be used with the psql CLI.");

                backup.AddOption(srcOpt);
                backup.AddOption(destOpt);

                backup.SetHandler(async (context) =>
                {
                    var inputs = new Inputs
                    {
                        SourceDb = context.ParseResult.GetValueForOption(srcOpt),
                        DestPath = context.ParseResult.GetValueForOption(destOpt)
                    };

                    await Execute(inputs, sp.GetService<IConfiguration>(), context.Console);
                });

                return backup;
            });

            return builder;
        }

        private static async Task<int> Execute(Inputs inputs, IConfiguration config, IConsole console)
        {
            string sourceDb = GetConnectionString(inputs.SourceDb, config);
            if (sourceDb == null)
            {
                console.Error.WriteLine($"Unable to resolve connection string.");
                return ExitCodes.Error;
            }

            var connectionError = await TryOpenConnection(sourceDb);
            if (connectionError != null)
            {
                console.Error.WriteLine($"Unable to connect to database: [{inputs.SourceDb}]. Reason: {connectionError.Message}");
                return ExitCodes.Error;
            }

            var pgDump = Postgres.Run
                .PgDump(sourceDb, inputs.DestPath, config)
                .SetStdOut(console.Out);

            console.WriteLine("Performing backup...");

            using (var process = pgDump.Start())
            {
                await process.WaitForExitAsync();

                if (process.ExitCode != ExitCodes.Success)
                {
                    string stderr = await process.StandardError.ReadToEndAsync();
                    console.Error.WriteLine($"Backup failed. Reason: {stderr}");
                    return process.ExitCode;
                }

                string stdout = await process.StandardOutput.ReadToEndAsync();
                console.WriteLine(stdout);
                return ExitCodes.Success;
            }
        }

        private static async Task<Exception> TryOpenConnection(string connectionString)
        {
            var connStr = new NpgsqlConnectionStringBuilder(connectionString);
            connStr.Timeout = 5;

            using (var conn = new NpgsqlConnection(connStr.ToString()))
            {
                try
                {
                    await conn.OpenAsync();

                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }
        }

        private static string GetConnectionString(string source, IConfiguration config)
        {
            string result = config.GetConnectionString(source);

            return result == null ? source : result;
        }

        public class Inputs
        {
            public string SourceDb { get; set; }
            public string DestPath { get; set; }
        }
    }
}
