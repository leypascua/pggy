using Npgsql;
using Pggy.Cli.Commands;
using Pggy.Cli.Infrastructure;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Postgres
{
    public static class Helpers
    {
        public static bool IsOnSameHost(this NpgsqlConnectionStringBuilder first, NpgsqlConnectionStringBuilder second)
        {
            Func<NpgsqlConnectionStringBuilder, string> buildHost = (csb) =>
            {
                string port = csb.Port == 0  ? "5432" : csb.Port.ToString();
                return $"{csb.Host}:{port}";
            };

            return buildHost(first) == buildHost(second);
        }

        public static async Task<bool> DropAndCreateDatabase(this NpgsqlConnectionStringBuilder csb, IConsole console, bool isForced = false, string withTemplateDbName = null)
        {
            var connStr = new NpgsqlConnectionStringBuilder(csb.ToString());
            bool useTemplate = !string.IsNullOrEmpty(withTemplateDbName);

            connStr.Timeout = 5;
            connStr.Pooling = false;
            connStr.Database = withTemplateDbName ?? Constants.DEFAULT_USER;

            if (useTemplate)
            {
                connStr.CommandTimeout = 0;
            }

            using (var conn = new NpgsqlConnection(connStr.ToString()))
            {
                conn.Open();

                if (!isForced)
                {
                    console.WriteLine($"  *** WARNING! This destructive command will drop the database [{csb.Database}] on server [{csb.Host}]");
                    console.WriteLine($"      This operation is irreversible. Press [CTRL+C] now to abort...\r\n");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                console.WriteLine($"  > Dropping database [{csb.Database}]...");

                // kill processes
                await KillDbProcesses(csb.Database, conn);

                // drop database
                var dropCmd = conn.CreateCommand();
                dropCmd.CommandText = $"DROP DATABASE IF EXISTS {csb.Database};";
                await dropCmd.ExecuteNonQueryAsync();


                string template = string.Empty;

                if (useTemplate)
                {
                    await KillDbProcesses(withTemplateDbName, conn);
                    template = $" TEMPLATE {withTemplateDbName}";
                }

                // create database
                var createCmd = conn.CreateCommand();
                string commandText = $"CREATE DATABASE {csb.Database} WITH OWNER {csb.Username}{template};";
                console.WriteLine($"  > {commandText}");
                createCmd.CommandText = commandText;
                await createCmd.ExecuteNonQueryAsync();

                return true;
            }
        }

        private static async Task KillDbProcesses(string dbName, NpgsqlConnection conn)
        {
            var killCmd = conn.CreateCommand();
            killCmd.CommandText = typeof(RestoreCommand).Assembly.GetManifestResourceString("Pggy.Cli.Resources.killprocesses.command.sql");
            killCmd.Parameters.Add(new() { ParameterName = "dbName", Value = dbName });
            await killCmd.ExecuteNonQueryAsync();
        }
    }
}
