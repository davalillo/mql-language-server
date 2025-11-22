using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Serilog;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Mql4LanguageServer
{
    /// <summary>
    /// Entry point for the MQL4 Language Server
    ///
    /// Phase 3.7: Complete LSP Server with stdio connection and all handlers
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Use AppContext.BaseDirectory for single-file apps compatibility
            var executablePath = System.IO.Path.Combine(AppContext.BaseDirectory, "mql4-lsp-server");
            var buildDate = System.IO.File.GetLastWriteTime(executablePath);

            // Check for --version flag first
            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;

                Console.WriteLine($"MQL4 Language Server v{version?.Major}.{version?.Minor}.{version?.Build}");
                Console.WriteLine($"Build Date: {buildDate:yyyy-MM-dd HH:mm:ss}");
                return 0;
            }

            // Configure Serilog for structured logging
            // IMPORTANT: Write to stderr to avoid polluting JSON-RPC stdout
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(
                    standardErrorFromLevel: Serilog.Events.LogEventLevel.Information,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("mql4-lsp-server.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("=================================================");
                Log.Information("Starting MQL4 Language Server");
                Log.Information($"Build Date: {buildDate:yyyy-MM-dd HH:mm:ss}");
                Log.Information("Phase 3.7: Complete LSP Server Implementation");
                Log.Information("=================================================");

                // Create Language Server with stdio transport
                var server = LanguageServer.Create(options =>
                {
                    options
                        .WithInput(Console.OpenStandardInput())
                        .WithOutput(Console.OpenStandardOutput())
                        .WithLoggerFactory(LoggerFactory.Create(builder => builder.AddSerilog()))
                        .WithServices(services =>
                        {
                            // Register parser
                            services.AddSingleton<Mql4AntlrParser>();

                            // Register document store for tracking open files
                            services.AddSingleton<OpenDocumentStore>();

                            // Register all handlers
                            services.AddSingleton<DocumentSymbolHandler>();
                            services.AddSingleton<DefinitionHandler>();
                            services.AddSingleton<ReferencesHandler>();
                            services.AddSingleton<CompletionHandler>();
                            services.AddSingleton<HoverHandler>();
                            services.AddSingleton<DidOpenTextDocumentHandler>();
                            services.AddSingleton<DidCloseTextDocumentHandler>();
                            services.AddSingleton<DidChangeTextDocumentHandler>();

                            // Register LSP server
                            services.AddSingleton<Mql4LspServer>();
                        })
                        // Explicitly register handlers with OmniSharp
                        .WithHandler<DocumentSymbolHandler>()
                        .WithHandler<DefinitionHandler>()
                        .WithHandler<ReferencesHandler>()
                        .WithHandler<CompletionHandler>()
                        .WithHandler<HoverHandler>()
                        .WithHandler<DidOpenTextDocumentHandler>()
                        .WithHandler<DidCloseTextDocumentHandler>()
                        .WithHandler<DidChangeTextDocumentHandler>();
                });

                Log.Information("Language Server created successfully");
                Log.Information("All LSP Handlers registered:");
                Log.Information("  - DocumentSymbolHandler (Outline View)");
                Log.Information("  - DefinitionHandler (Go-to-Definition)");
                Log.Information("  - ReferencesHandler (Find All References)");
                Log.Information("  - CompletionHandler (Auto-completion)");
                Log.Information("  - HoverHandler (Symbol Information)");
                Log.Information("  - TextDocumentSync Handlers (Open/Close/Change)");

                Log.Information("About to call server.Initialize()...");
                await Task.Delay(100);  // Give time for log to flush

                // Initialize with timeout for testing - in production, client connects immediately
                try
                {
                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(default, CancellationToken.None))
                    {
                        cts.CancelAfter(TimeSpan.FromSeconds(5));
                        await server.Initialize(cts.Token);
                    }
                    Log.Information("server.Initialize() completed - client connected!");

                    // Send experimental/serverStatus notification after initialization
                    server.SendNotification("experimental/serverStatus", new
                    {
                        quiescent = true
                    });
                    Log.Information("Sent experimental/serverStatus notification (quiescent: true)");
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("server.Initialize() timed out - no LSP client connected. This is expected when running manually.");
                    Log.Information("In production, the LSP client (VSCode, Neovim, etc.) will connect automatically.");
                }

                Log.Information("server.Initialize() process finished!");

                Log.Information("=================================================");
                Log.Information("MQL4 Language Server is ready");
                Log.Information("Listening on stdio...");
                Log.Information("=================================================");

                // Wait for the server to shutdown when the client disconnects
                await Task.Delay(Timeout.Infinite, CancellationToken.None);

                Log.Information("MQL4 Language Server shutting down...");

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "MQL4 Language Server terminated unexpectedly");
                return 1;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
