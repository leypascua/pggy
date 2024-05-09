using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pggy.Cli.Infrastructure;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
                var forceOpt = new Option<bool>("--force", "Perform the command right away, without delay");

                restore.AddOption(dumpOpt);
                restore.AddOption(targetOpt);
                restore.AddOption(forceOpt);

                restore.SetHandler(async (context) =>
                {
                    var inputs = new Inputs
                    {
                        DumpFile = context.ParseResult.GetValueForOption(dumpOpt),
                        TargetDb = context.ParseResult.GetValueForOption(targetOpt),
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

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            bool isDatabaseDropped = await DropDatabase(csb, console, inputs.IsForced);
            if (!isDatabaseDropped)
            {
                return ExitCodes.Success;
            }

            int exitCode = await RestoreFromDump(dbDump, csb, config, console);

            console.WriteLine($"\r\nDone after {stopwatch.Elapsed.Humanize()}");
            return exitCode;
        }

        private static async Task<int> RestoreFromDump(FileInfo dumpFile, NpgsqlConnectionStringBuilder csb, IConfiguration config, IConsole console)
        {
            var psql = Postgres.Run
                .Psql(csb, config)
                .RedirectStdIn(true);

            console.WriteLine($"  > Now restoring database [{csb.Database}] from dump [{dumpFile.Name}]...\r\n");

            using (FileStream dumpStream = dumpFile.OpenRead())
            using (var gzipStream = new GZipStream(dumpStream, CompressionMode.Decompress))
            using (var process = psql.Start())
            {
                var readBuffer = new byte[512];
                int bytesRead = 0;

                while ((bytesRead = gzipStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    string content = Encoding.UTF8.GetString(readBuffer, 0, bytesRead);

                    if (process.HasExited)
                    {
                        string errorMessage = await process.StandardError.ReadToEndAsync();
                        console.Error.WriteLine("  > ERROR: Process has terminated. REASON: " + errorMessage);
                        return ExitCodes.Error;
                    }

                    process.StandardInput.Write(content);
                    process.StandardInput.Flush();
                }

                if (!process.HasExited)
                {
                    await process.StandardInput.WriteLineAsync("\\q");
                }

                await process.WaitForExitAsync();

                if (process.ExitCode != ExitCodes.Success)
                {
                    string stderr = await process.StandardError.ReadToEndAsync();
                    console.Error.WriteLine($"Restore failed. Reason: {stderr}");
                    return process.ExitCode;
                }
            }

            return ExitCodes.Success;
        }

        private static async Task<bool> DropDatabase(NpgsqlConnectionStringBuilder csb, IConsole console, bool isForced = false)
        {
            var connStr = new NpgsqlConnectionStringBuilder(csb.ToString());

            connStr.Timeout = 5;
            connStr.Pooling = false;
            connStr.Database = "postgres";

            using (var conn = new NpgsqlConnection(connStr.ToString()))
            {
                conn.Open();

                if (!isForced)
                {
                    console.WriteLine($"*** WARNING! This destructive command will drop the database [{csb.Database}] on server [{csb.Host}] which cannot be reversed. Press [CTRL+C] now to abort...\r\n");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                console.WriteLine($"  > Dropping database [{csb.Database}]...");

                // kill processes
                var killCmd = conn.CreateCommand();
                killCmd.CommandText = typeof(RestoreCommand).Assembly.GetManifestResourceString("Pggy.Cli.Resources.killprocesses.command.sql");
                killCmd.Parameters.Add(new() { ParameterName = "dbName", Value = csb.Database });
                await killCmd.ExecuteNonQueryAsync();

                // drop database
                var dropCmd = conn.CreateCommand();
                dropCmd.CommandText = $"DROP DATABASE IF EXISTS {csb.Database};";
                await dropCmd.ExecuteNonQueryAsync();

                // create database
                var createCmd = conn.CreateCommand();
                createCmd.CommandText = $"CREATE DATABASE {csb.Database} WITH OWNER {csb.Username};";
                await createCmd.ExecuteNonQueryAsync();

                return true;
            }
        }

        public class Inputs
        {
            public string DumpFile { get; set; }
            public string TargetDb { get; set; }
            public bool IsForced { get; set; }
        }
    }
}
