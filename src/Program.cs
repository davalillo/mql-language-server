using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Serilog;

using OmniSharp.Extensions.LanguageServer.Server;

using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace MqlLanguageServer
{
    /// <summary>
    /// Entry point for the MQL Language Server
    ///
    /// Phase 3.7: Complete LSP Server with stdio connection and all handlers
    /// </summary>
    class Program
    {
        // Captured in OnInitialize (WI-01/D6) and consumed after LanguageServer.From
        // completes, so the initialize response is never delayed by the scan.
        private static List<string> workspaceFoldersSnapshot = new();

        /// <summary>
        /// Reads the build date embedded as assembly metadata ("BuildDate") by the
        /// 'StampBuildDate' MSBuild target. Falls back to "unknown" when the metadata
        /// is missing (e.g. assemblies produced before the target existed).
        /// </summary>
        private static string GetBuildDate()
        {
            var value = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "BuildDate")
                ?.Value;

            return string.IsNullOrEmpty(value) ? "unknown" : value;
        }

        static async Task<int> Main(string[] args)
        {
            // Build date is embedded as assembly metadata by the 'StampBuildDate'
            // MSBuild target (src/MqlLanguageServer.Server.csproj). It reflects the
            // UTC time of the last successful compile of this assembly.
            var buildDateStr = GetBuildDate();

            // Check for --version flag first.
            // Version comes from the assembly (issue #22), same value shipped in the package.
            var versionStr = ServerVersion.Version;
            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
            {
                Console.WriteLine($"MQL Language Server {versionStr}");
                Console.WriteLine($"Build Date: {buildDateStr}");
                return 0;
            }

            // Configure Serilog for structured logging
            // IMPORTANT: Write to stderr to avoid polluting JSON-RPC stdout
            //
            // Issue #21c: the file sink used to write into the process working
            // directory (often the workspace root) with unbounded daily-file
            // retention. Logs now go to the OS-appropriate per-user log
            // directory, created up front if missing, with capped retention
            // (7 daily files, 20 MB each) and shared mode so several editor
            // windows can run against the same directory.
            LogPaths.EnsureLogDirectoryExists();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(
                    standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    LogPaths.GetLogFilePath(),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 20 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    shared: true)
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
                            services.AddTransient<Mql5AntlrParser>();
                            services.AddSingleton<OpenDocumentStore>();
                            services.AddSingleton<GlobalSymbolIndex>();
                            // Issue #25c: handlers receive the shared index
                            // through this accessor instead of touching the
                            // static GlobalSymbolIndex.Instance singleton.
                            services.AddSingleton<GlobalSymbolIndexAccessor>();
                            services.AddSingleton<MetricsCollector>();
                            services.AddSingleton<MqlLspServer>();
                            services.AddSingleton<MqlLanguageService>();
                            services.AddSingleton<WorkspaceIndexer>();

                            // Per-language built-in registries (IMqlBuiltins[]) are injected into handlers.
                            services.AddSingleton<IMqlBuiltins, Mql4BuiltinsAdapter>();
                            services.AddSingleton<IMqlBuiltins, Mql5Builtins>();

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
                        .WithHandler<DiagnosticHandler>()

                        // Handlers de sincronización de texto
                        .WithHandler<DidOpenTextDocumentHandler>()
                        .WithHandler<DidCloseTextDocumentHandler>()
                        .WithHandler<DidChangeTextDocumentHandler>()
                        // Report the real version in the LSP initialize response
                        // (ServerInfo). The value is derived from the assembly, which
                        // the MSBuild SDK stamps from <Version> in the csproj — the
                        // same value the CI tag↔csproj gate enforces (issue #22).
                        .WithServerInfo(new ServerInfo
                        {
                            Name = Constants.Server.Name,
                            Version = ServerVersion.Version
                        })
                        .OnInitialize((server, request, token) =>
                        {
                            Log.Information("MQL Language Server initialized for client: {ClientName}", request.ClientInfo?.Name ?? "unknown");

                            // WI-01/D6: capture workspace folders during OnInitialize;
                            // the scan starts AFTER InitializeResult is computed so the
                            // response is never delayed (WI-02). Fire-and-forget.
                            workspaceFoldersSnapshot = new List<string>();
                            var foldersContainer = request.WorkspaceFolders;
                            if (foldersContainer is not null)
                            {
                                foreach (var folder in foldersContainer)
                                {
                                    var path = folder.Uri.ToUri().AbsolutePath;
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        workspaceFoldersSnapshot.Add(path);
                                    }
                                }
                            }

                            // Issue #18: register the declared roots for the central
                            // read guard in SourceFileReader. From this point on,
                            // every client-driven disk read must be inside one of
                            // them (fail-open only while the set is empty).
                            WorkspaceRoots.Set(workspaceFoldersSnapshot);

                            // The InitializeResult for this handler's return type is
                            // built by the library from options.ServerInfo (set below
                            // via WithServerInfo, issue #22); this delegate's return
                            // value is ignored by OmniSharp 0.19.9 (Task-returning
                            // delegate), so capabilities are derived from the
                            // registered handlers.
                            return Task.CompletedTask;
                        })

                        // Esto está bien para forzar la configuración de sync
                        .OnTextDocumentSync(
                            TextDocumentSyncKind.Full,
                            uri => new TextDocumentAttributes(uri, LanguageDetection.GetLanguageIdFromUri(uri.ToUri())),
                            _ => { }, _ => { }, _ => { }, _ => { },
                            new TextDocumentSyncRegistrationOptions())
                        
                            ;
                });

                Log.Information("Language Server started and listening on stdio...");

                // WI-01/D6: start the workspace scan after initialization so the
                // initialize response was never delayed (WI-02). Fire-and-forget.
                try
                {
                    var workspaceFolders = workspaceFoldersSnapshot;
                    if (workspaceFolders.Count > 0)
                    {
                        server.GetRequiredService<WorkspaceIndexer>().StartIndexing(workspaceFolders);
                        Log.Information("Workspace scan started for {FolderCount} folder(s)", workspaceFolders.Count);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to start workspace scan; server continues without it.");
                }

                // CAMBIO 4: Esperar a que termine
                await server.WaitForExit;

                // Cancel the workspace scan on shutdown (D6).
                try
                {
                    server.GetRequiredService<WorkspaceIndexer>().StopIndexing();
                }
                catch (ObjectDisposedException)
                {
                    // server already disposed during shutdown — nothing to cancel
                }

                // Log metrics summary before shutdown
                var metrics = MetricsCollector.Instance.GetSummaryReport();
                Log.Information("\n{MetricsSummary}", metrics);

                Log.Information("MQL Language Server shutting down...");


                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "MQL Language Server terminated unexpectedly");
                return 1;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}