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
    using Pggy.Cli.Postgres;
    using System.Diagnostics;
    using System.IO.Compression;

    public static class BackupCommand
    {
        public static ConsoleAppBuilder AddBackupCommand(this ConsoleAppBuilder builder)
        {
            builder.AddCommand(sp =>
            {
                var backup = new Command("backup", "Backup a source database into a file.");

                var srcOpt = new Option<string>("--src", "A Npgsql connection string (or name of connection in the ConnectionStrings section of the config file) of the source db");
                var destOpt = new Option<string>("--dest", "The destination path of the resulting DB dump file that can be used with the psql CLI.");
                var compOpt = new Option<CompressionMethod>("--compression", "[gz | br] Optional. The compression method to use. Defaults to [gz]ip. Use [br]otli for best results");
                compOpt.SetDefaultValue("gz");

                backup.AddOption(srcOpt);
                backup.AddOption(destOpt);
                backup.AddOption(compOpt);

                backup.SetHandler(async (context) =>
                {
                    var inputs = new Inputs
                    {
                        SourceDb = context.ParseResult.GetValueForOption(srcOpt),
                        DestPath = context.ParseResult.GetValueForOption(destOpt),
                        CompressionMethod = context.ParseResult.GetValueForOption(compOpt),
                    };

                    context.ExitCode = await Execute(inputs, sp.GetService<IConfiguration>(), context.Console);
                });

                return backup;
            });

            return builder;
        }

        private static async Task<int> Execute(Inputs inputs, IConfiguration config, IConsole console)
        {
            var csb = config.GetNpgsqlConnectionString(inputs.SourceDb);
            if (csb == null)
            {
                if (!inputs.SourceDb.IsValidConnectionString())
                {
                    console.Error.WriteLine($"  > Invalid connection string received: [{inputs.SourceDb}]");
                    return ExitCodes.Error;
                };

                csb = new NpgsqlConnectionStringBuilder(inputs.SourceDb);
            }

            var connectionError = await TryOpenConnection(csb);
            if (connectionError != null)
            {
                console.Error.WriteLine($"Unable to connect to database: [{inputs.SourceDb}]. Reason: {connectionError.Message}");
                return ExitCodes.Error;
            }

            var pgDump = Postgres.Run
                .PgDump(csb, config);

            string ext = inputs.CompressionMethod.ToString().ToLowerInvariant();
            string filename = $"{csb.Database}.{DateTime.UtcNow.ToString("yyyyMMddThhmm")}.sql.{ext}";
            string dumpDir = GetValidDestinationPath(inputs.DestPath);
            string finalDumpPath = Path.Combine(dumpDir, filename);

            console.WriteLine($"Starting pg_dump on database [{csb.Database}] on host= [{csb.Host}]...");
            var stopwatch = new Stopwatch();
            stopwatch.Start();


            using (FileStream outStream = File.Create(finalDumpPath))
            using (var packageStream = PackageStream.CreateWith(outStream))
            using (var process = pgDump.Start())
            {
                var charBuffer = new char[1 * 1024 * 1024];

                while (!process.HasExited)
                {
                    int charsRead = 0;
                    while ((charsRead = await process.StandardOutput.ReadAsync(charBuffer, 0, charBuffer.Length)) > 0)
                    {
                        var byteBuffer = Encoding.UTF8.GetBytes(charBuffer, 0, charsRead);
                        await packageStream.WriteAsync(byteBuffer, 0, byteBuffer.Length);
                        await packageStream.FlushAsync();
                    }
                }
                
                packageStream.Close();

                if (process.ExitCode != ExitCodes.Success)
                {
                    string stderr = await process.StandardError.ReadToEndAsync();
                    console.Error.WriteLine($"Backup failed. Reason: {stderr}");
                    return process.ExitCode;
                }
            }

            console.WriteLine($"\r\nDone after {stopwatch.Elapsed.Humanize()}");
            return ExitCodes.Success;
        }

        private static string GetValidDestinationPath(string path)
        {
            string finalPath = Path.IsPathRooted(path) ?
                path :
                Path.Combine(Environment.CurrentDirectory, path);

            if (!Directory.Exists(finalPath))
            {
                Directory.CreateDirectory(finalPath);
            }

            return finalPath;
        }

        private static async Task<Exception> TryOpenConnection(NpgsqlConnectionStringBuilder connectionString)
        {
            connectionString.Timeout = 5;

            using (var conn = new NpgsqlConnection(connectionString.ToString()))
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

        public enum CompressionMethod
        {
            gz = 0,
            br = 1
        }

        public class Inputs
        {
            public string SourceDb { get; set; }
            public string DestPath { get; set; }
            public CompressionMethod CompressionMethod { get; set; }
        }
    }
}
