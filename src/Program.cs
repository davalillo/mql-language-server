using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Serilog;

using OmniSharp.Extensions.LanguageServer.Server;

using OmniSharp.Extensions.LanguageServer.Protocol.Document;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace MqlLanguageServer
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
            // Get build date from BuildConstants (immutable, embedded at compile time)
            var buildDateStr = BuildConstants.BuildDate;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "unknown";

            // Check for --version flag first
            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
            {
                Console.WriteLine($"MQL Language Server {versionStr}");
                Console.WriteLine($"Build Date: {buildDateStr}");
                return 0;
            }

            // Configure Serilog for structured logging
            // IMPORTANT: Write to stderr to avoid polluting JSON-RPC stdout
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(
                    standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("mql-lsp-server.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("=================================================");
                Log.Information("Starting MQL Language Server");
                Log.Information($"Version: {versionStr}");
                Log.Information($"Build Date: {buildDateStr}");
                Log.Information("Phase 3.7: Complete LSP Server Implementation");
                Log.Information("=================================================");

                // CAMBIO 1: Usamos 'From' en lugar de 'Create'. Es asíncrono.
                // Esto garantiza que el servidor se inicialice correctamente.
                var server = await LanguageServer.From(options =>
                {
                    _ = options
                        //.WithInput(Console.OpenStandardInput())
                        .WithInput(new DisconnectAwareStream(Console.OpenStandardInput()))
                        .WithOutput(Console.OpenStandardOutput())
                        .WithLoggerFactory(LoggerFactory.Create(builder => builder.AddSerilog()))
                        .WithServices(services =>
                        {
                            // MANTÉN SOLO TUS SERVICIOS PROPIOS
                            services.AddTransient<Mql4AntlrParser>();
                            services.AddSingleton<OpenDocumentStore>();
                            services.AddSingleton<GlobalSymbolIndex>();
                            services.AddSingleton<MetricsCollector>();
                            services.AddSingleton<MqlLspServer>();

                            // CAMBIO 2: ¡ELIMINA TODOS LOS AddSingleton DE HANDLERS AQUÍ!
                            // .WithHandler<T>() se encarga de registrarlos en la DI automáticamente.
                        })
                        // CAMBIO 3: Registra los Handlers. La librería detectará sus interfaces
                        // y llenará las Capabilities (hoverProvider: true, etc.) por ti.
                        .WithHandler<DocumentSymbolHandler>()
                        .WithHandler<DefinitionHandler>()
                        .WithHandler<ReferencesHandler>()
                        .WithHandler<CompletionHandler>()
                        .WithHandler<HoverHandler>()
                        .WithHandler<RenameHandler>()
                        .WithHandler<SignatureHelpHandler>()
                        .WithHandler<FoldingRangeHandler>()
                        .WithHandler<SelectionRangeHandler>()
                        .WithHandler<DocumentHighlightHandler>()
                        .WithHandler<DocumentFormattingHandler>()
                        .WithHandler<RangeFormattingHandler>()
                        .WithHandler<TypeDefinitionHandler>()
                        .WithHandler<CodeActionHandler>()
                        .WithHandler<CodeActionResolveHandler>()
                        .WithHandler<DeclarationHandler>()
                        .WithHandler<ImplementationHandler>()
                        .WithHandler<WorkspaceSymbolHandler>()
                        // .WithHandler<DiagnosticHandler>()

                        // Handlers de sincronización de texto
                        .WithHandler<DidOpenTextDocumentHandler>()
                        .WithHandler<DidCloseTextDocumentHandler>()
                        .WithHandler<DidChangeTextDocumentHandler>()
                        .OnInitialize((server, request, token) =>
                        {
                            Log.Information("MQL Language Server initialized for client: {ClientName}", request.ClientInfo?.Name ?? "unknown");
                            return Task.FromResult(new InitializeResult
                            {
                                Capabilities = new ServerCapabilities
                                {
                                    // Forzamos que aparezca True (o el objeto de opciones)
                                    WorkspaceSymbolProvider = true,

                                    
                                }
                            });
                        })

                        // Esto está bien para forzar la configuración de sync
                        .OnTextDocumentSync(
                            TextDocumentSyncKind.Full,
                            uri => new TextDocumentAttributes(uri, "mql4"),
                            _ => { }, _ => { }, _ => { }, _ => { },
                            new TextDocumentSyncRegistrationOptions())
                        
                            ;
                });

                Log.Information("Language Server started and listening on stdio...");

                // CAMBIO 4: Esperar a que termine
                await server.WaitForExit;

                // Log metrics summary before shutdown
                var metrics = MetricsCollector.Instance.GetSummaryReport();
                Log.Information("\n{MetricsSummary}", metrics);

                Log.Information("MQL Language Server shutting down...");


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