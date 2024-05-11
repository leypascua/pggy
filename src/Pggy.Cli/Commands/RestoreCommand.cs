using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pggy.Cli.Infrastructure;
using Pggy.Cli.Postgres;
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
        const int BUFFER_SIZE = 2097152;

        public static ConsoleAppBuilder AddRestoreCommand(this ConsoleAppBuilder builder)
        {
            builder.AddCommand(sp =>
            {
                var restore = new Command("restore", "Restore a (g)zipped archive plain text dump from pg_dump to a destination database");

                var dumpOpt = new Option<string>("--dump", "[dbdump.sql.gz | pgdump.sql.zip] A (g)zipped archive produced by pg_dump using the -Fp option.");
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
            var csb = new NpgsqlConnectionStringBuilderFactory(config)
                .CreateBuilderFrom(inputs.TargetDb);

            if (csb == null)
            {
                console.Error.WriteLine($"  > Invalid connection string received: [{inputs.TargetDb}]");
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

            bool isDatabaseDropped = await csb.DropAndCreateDatabase(console, inputs.IsForced);
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
            using (var packageStream = PackageStream.Open(dumpStream))
            using (var process = psql.Start())
            {
                var readBuffer = new byte[BUFFER_SIZE];
                int bytesRead = 0;

                while ((bytesRead = packageStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    string content = Encoding.UTF8.GetString(readBuffer, 0, bytesRead);

                    if (process.HasExited)
                    {
                        string errorMessage = await process.StandardError.ReadToEndAsync();
                        console.Error.WriteLine("  > ERROR: Process has terminated. REASON: " + errorMessage);
                        return ExitCodes.Error;
                    }

                    await process.StandardInput.WriteAsync(content);
                    await process.StandardInput.FlushAsync();
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

        public class Inputs
        {
            public string DumpFile { get; set; }
            public string TargetDb { get; set; }
            public bool IsForced { get; set; }
        }
    }
}
