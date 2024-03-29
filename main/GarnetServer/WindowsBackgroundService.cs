// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Garnet.server;
using Microsoft.Extensions.Hosting;

namespace Garnet
{
    public sealed class WindowsBackgroundService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var args = new[] { "--storage-tier", "-r", "--aof" };
            try
            {
                using var server = new GarnetServer(args);

                // Optional: register custom extensions
                RegisterExtensions(server);

                // Start the server
                server.Start();

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
            }
            catch (Exception ex)
            {
                // Terminates this process and returns an exit code to the operating system.
                // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
                // performs one of two scenarios:
                // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
                // 2. When set to "StopHost": will cleanly stop the host, and log errors.
                //
                // In order for the Windows Service Management system to leverage configured
                // recovery options, we need to terminate the process with a non-zero exit code.
                Console.WriteLine($"Unable to initialize server due to exception: {ex.Message}");
                Environment.Exit(1);
            }
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