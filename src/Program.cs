using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Parser;
using Serilog;
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
            // Configure Serilog for structured logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("mql4-lsp-server.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("=================================================");
                Log.Information("Starting MQL4 Language Server");
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
                        });
                });

                Log.Information("Language Server created successfully");
                Log.Information("All LSP Handlers registered:");
                Log.Information("  - DocumentSymbolHandler (Outline View)");
                Log.Information("  - DefinitionHandler (Go-to-Definition)");
                Log.Information("  - ReferencesHandler (Find All References)");
                Log.Information("  - CompletionHandler (Auto-completion)");
                Log.Information("  - HoverHandler (Symbol Information)");
                Log.Information("  - TextDocumentSync Handlers (Open/Close/Change)");

                // Initialize the LSP server
                await server.Initialize(default);

                Log.Information("=================================================");
                Log.Information("MQL4 Language Server is ready");
                Log.Information("Listening on stdio...");
                Log.Information("=================================================");

                // Wait for disconnect - this keeps the server running
                await Task.Delay(-1, CancellationToken.None);

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
