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
using Pggy.Cli.Postgres;
using Npgsql;
using System.Diagnostics;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace Pggy.Cli.Commands
{
    public static class CopyCommand
    {
        public static ConsoleAppBuilder AddCopyCommand(this ConsoleAppBuilder builder)
        {
            builder.AddCommand(sp =>
            {
                var restore = new Command("copy", "Copy a source database into a destination database");

                var sourceOpt = new Option<string>("--src", "A Npgsql connection string (or name of connection in the ConnectionStrings section of the config file) of the source db");
                var destOpt = new Option<string>("--dest", "A Npgsql connection string (or name of connection in the ConnectionStrings section of the config file) of the destination db");
                var dumpOpt = new Option<string>("--dump", "[/path/to/dump] Optional. The destination path of the database dump file that can be used to restore the copied database elsewhere. Only works when source and dest are in separate hosts.");
                var forceOpt = new Option<bool>("--force", "Perform the command right away, without delay");

                restore.AddOption(sourceOpt);
                restore.AddOption(destOpt);
                restore.AddOption(dumpOpt);
                restore.AddOption(forceOpt);

                restore.SetHandler(async (context) =>
                {
                    var inputs = new Inputs
                    {
                        SourceDb = context.ParseResult.GetValueForOption(sourceOpt),
                        DestDb = context.ParseResult.GetValueForOption(destOpt),
                        DumpPath = context.ParseResult.GetValueForOption(dumpOpt),
                        IsForced = context.ParseResult.GetValueForOption(forceOpt)
                    };

                    context.ExitCode = await Execute(inputs, sp.GetService<IConfiguration>(), context.Console);
                });

                return restore;
            });

            return builder;
        }

        private static (NpgsqlConnectionStringBuilder, NpgsqlConnectionStringBuilder) ResolveConnectionStrings(IConfiguration config, string sourceDb, string destDb)
        {   
            var factory = new NpgsqlConnectionStringBuilderFactory(config);

            NpgsqlConnectionStringBuilder source, dest;

            source = factory.CreateBuilderFrom(sourceDb);
            dest = factory.CreateBuilderFrom(destDb);

            return (source, dest);
        }

        private static async Task<int> Execute(Inputs inputs, IConfiguration config, IConsole console)
        {
            // get source and destination connection strings
            var (sourceDb, destDb) = ResolveConnectionStrings(config, inputs.SourceDb, inputs.DestDb);

            if (sourceDb == null)
            {
                console.Error.WriteLine($"  > Invalid source connection string received: [{inputs.SourceDb}]");
                return ExitCodes.Error;
            }

            if (destDb == null)
            {
                console.Error.WriteLine($"  > Invalid source connection string received: [{inputs.DestDb}]");
                return ExitCodes.Error;
            }

            bool isSameDb = sourceDb.IsOnSameHost(destDb) && sourceDb.Database == destDb.Database;
            if (isSameDb)
            {
                console.Error.WriteLine("  > Source and destination databases are the same.");
                return ExitCodes.Error;
            }

            console.WriteLine($"  > Copying source db [{sourceDb.Database}] ({sourceDb.Host}) into destination db [{destDb.Database}] ({destDb.Host})...\r\n");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int exitCode = sourceDb.IsOnSameHost(destDb) ?
                await CopyWithTemplate(sourceDb, destDb, inputs, config, console) :
                await CopyWithPgDump(sourceDb, destDb, inputs, config, console);

            console.WriteLine($"\r\nDone after {stopwatch.Elapsed.Humanize()}");
            return exitCode;
        }

        private static async Task<int> CopyWithPgDump(NpgsqlConnectionStringBuilder sourceDb, NpgsqlConnectionStringBuilder destDb, Inputs inputs, IConfiguration config, IConsole console)
        {
            // use PG_DUMP to pipe the source DB into the dest DB via a PSQL process.

            var pgDump = Postgres.Run
                .PgDump(sourceDb, config);

            var psql = Postgres.Run
                .Psql(destDb, config)
                .RedirectStdIn(true)
                .SetStdOut(null);

            await destDb.DropAndCreateDatabase(console, inputs.IsForced);

            using (var pgDumpPid = pgDump.Start())
            using (var psqlPid = psql.Start())
            using (var dumpFile = new PgDumpFile(sourceDb.Database, inputs.DumpPath))
            {
                var charBuffer = new char[1024 * 1024];
                int charsRead = 0;

                while ((charsRead = await pgDumpPid.StandardOutput.ReadAsync(charBuffer, 0, charBuffer.Length)) > 0)
                {
                    if (psqlPid.HasExited)
                    {
                        console.Error.WriteLine($"PSQL process terminated unexpectedly.");
                        return psqlPid.ExitCode;
                    }

                    await psqlPid.StandardInput.WriteAsync(charBuffer, 0, charsRead);
                    await dumpFile.WriteAsync(charBuffer, 0, charsRead);
                }
            }

            return await ValueTask.FromResult(ExitCodes.Success);
        }

        private static async Task<int> CopyWithTemplate(NpgsqlConnectionStringBuilder sourceDb, NpgsqlConnectionStringBuilder destDb, Inputs inputs, IConfiguration config, IConsole console)
        {
            // use `CREATE DATABASE {dest.Database} WITH TEMPLATE {source.Database} OWNER {dest.Username};`

            if (sourceDb.Username != sourceDb.Username)
            {
                console.WriteLine("  > Unable to perform COPY on the same host when PSQL logins of source and destination databases are different.");
                return ExitCodes.Error;
            }

            await destDb.DropAndCreateDatabase(console, inputs.IsForced, withTemplateDbName: sourceDb.Database);

            return ExitCodes.Success;
        }

        class PgDumpFile : IDisposable
        {
            private readonly string _sourceDatabaseName;
            private readonly string _destPath;
            private FileStream _file = null;
            private Stream _packageStream = null;

            public PgDumpFile(string sourceDatabase, string destPath)
            {
                _sourceDatabaseName = sourceDatabase;
                _destPath = destPath;
            }

            public async Task WriteAsync(char[] buffer, int offset, int length)
            {
                Stream packStream = GetActiveStream();

                if (packStream == null) return;

                var byteBuffer = Encoding.UTF8.GetBytes(buffer, 0, length);
                await packStream.WriteAsync(byteBuffer, 0, byteBuffer.Length);
                await packStream.FlushAsync();
            }

            private Stream GetActiveStream()
            {
                if (_destPath == null) return null;
                
                if (_packageStream == null)
                {
                    if (!Directory.Exists(_destPath))
                    {
                        Directory.CreateDirectory(_destPath);
                    }

                    // using brotli compression for best results
                    string filename = $"{_sourceDatabaseName}.{DateTime.UtcNow.ToString("yyyyMMddTHHmm")}.sql.br";
                    string finalDumpPath = Path.Combine(_destPath, filename);

                    _file = File.Create(finalDumpPath);
                    _packageStream = PackageStream.CreateWith(_file);
                }

                return _packageStream;
            }

            public void Dispose()
            {
                // prune older dumps if they exist on the path.
                if (_destPath != null && Directory.Exists(_destPath) && _file != null)
                {
                    string latestGeneratedDumpFile = Path.GetFileName(_file.Name);
                    var dumps = Directory.EnumerateFiles(_destPath, $"{_sourceDatabaseName}.*.sql.br");
                    foreach (string file in dumps)
                    {
                        // do not delete the last generated file
                        if (Path.GetFileName(file) == latestGeneratedDumpFile) continue;

                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }
                }

                if (_packageStream != null)
                {
                    _packageStream.Close();
                    _packageStream.Dispose();
                    _packageStream = null;
                }

                if (_file != null)
                {
                    _file.Dispose();
                    _file = null;
                }
            }
        }

        public class Inputs
        {
            public string SourceDb { get; set; }
            public string DestDb { get; set; }
            public bool IsForced { get; set; }
            public string DumpPath { get; set; }
        }
    }
}
