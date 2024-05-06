﻿using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Pggy.Cli.Commands;
using Pggy.Cli.Infrastructure;

namespace Pggy.Cli
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            PrintBanner();

            var builder = ConsoleAppBuilder.Create("pggy: Backup and Copy PSQL databases");

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{EnvironmentContext.Name}.json", optional: true);

            builder.AddBackupCommand();

            var app = builder.Build();

            return await app.InvokeAsync(args);
        }

        private static void PrintBanner()
        {
            Console.WriteLine("\r\npggy: PSQL database backup and copy");
            Console.WriteLine("   Written by leypascua. All rights reserved.");
            Console.WriteLine();
        }
    }
}
