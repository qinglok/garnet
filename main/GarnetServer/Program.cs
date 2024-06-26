﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using Garnet.server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Garnet
{
    /// <summary>
    /// Garnet server entry point
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(@"D:\LQL\GarnetServer");
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "Garnet Server";
            });

            builder.Services.AddHostedService<WindowsBackgroundService>();


            var host = builder.Build();
            host.Run();
        }

        /// <summary>
        /// Register new commands with the server. You can access these commands from clients using
        /// commands such as db.Execute in StackExchange.Redis. Example:
        ///   db.Execute("SETIFPM", key, value, prefix);
        /// </summary>
        static void RegisterExtensions(GarnetServer server)
        {
            // Register custom command on raw strings (SETIFPM = "set if prefix match")
            server.Register.NewCommand("SETIFPM", 2, CommandType.ReadModifyWrite, new SetIfPMCustomCommand());

            // Register custom command on raw strings (SETWPIFPGT = "set with prefix, if prefix greater than")
            server.Register.NewCommand("SETWPIFPGT", 2, CommandType.ReadModifyWrite, new SetWPIFPGTCustomCommand());

            // Register custom command on raw strings (DELIFM = "delete if value matches")
            server.Register.NewCommand("DELIFM", 1, CommandType.ReadModifyWrite, new DeleteIfMatchCustomCommand());

            // Register custom commands on objects
            var factory = new MyDictFactory();
            server.Register.NewCommand("MYDICTSET", 2, CommandType.ReadModifyWrite, factory);
            server.Register.NewCommand("MYDICTGET", 1, CommandType.Read, factory);

            // Register stored procedure to run a transactional command
            server.Register.NewTransactionProc("READWRITETX", 3, () => new ReadWriteTxn());

            // Register stored procedure to run a non-transactional command
            server.Register.NewTransactionProc("GETTWOKEYSNOTXN", 2, () => new GetTwoKeysNoTxn());

            // Register sample transactional procedures
            server.Register.NewTransactionProc("SAMPLEUPDATETX", 8, () => new SampleUpdateTxn());
            server.Register.NewTransactionProc("SAMPLEDELETETX", 5, () => new SampleDeleteTxn());
        }
    }
}