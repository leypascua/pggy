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

        public static async Task<bool> DropDatabase(this NpgsqlConnectionStringBuilder csb, IConsole console, bool isForced = false)
        {
            var connStr = new NpgsqlConnectionStringBuilder(csb.ToString());

            connStr.Timeout = 5;
            connStr.Pooling = false;
            connStr.Database = Constants.DEFAULT_USER;

            using (var conn = new NpgsqlConnection(connStr.ToString()))
            {
                conn.Open();

                if (!isForced)
                {
                    console.WriteLine($"  *** WARNING! This destructive command will drop the database [{csb.Database}] on server [{csb.Host}] which cannot be reversed. Press [CTRL+C] now to abort...\r\n");
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
    }
}
