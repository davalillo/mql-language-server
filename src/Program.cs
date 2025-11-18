using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Mql4LanguageServer
{
    /// <summary>
    /// Entry point for the MQL4 Language Server
    ///
    /// Phase 3.5: Basic LSP Server Core implementation
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Configure Serilog for structured logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("mql4-lsp-server.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting MQL4 Language Server");
                Log.Information("Phase 3.5: LSP Server Core Implementation");

                // Create a simple service collection
                var services = new ServiceCollection()
                    .AddLogging(builder => builder.AddSerilog())
                    .AddSingleton<Lsp.Server.Mql4LspServer>()
                    .BuildServiceProvider();

                // Create and initialize the MQL4 LSP Server
                var server = services.GetRequiredService<Lsp.Server.Mql4LspServer>();
                server.Initialize();

                Log.Information("MQL4 Language Server initialized successfully");
                Log.Information("Server Core Complete - Handlers pending Phase 3.6");

                // For now, just exit - full LSP protocol implementation in Phase 3.6
                await Task.Delay(100);

                Log.Information("MQL4 Language Server shutting down");

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "MQL4 Language Server terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
