using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using MqlLanguageServer.Models;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;

namespace MqlLanguageServer.Lsp.Handlers;

public class DiagnosticHandler : IDocumentDiagnosticHandler
{
    private readonly ILogger<DiagnosticHandler> _logger;
    private readonly IServiceProvider _serviceProvider; // Usamos esto para crear Parsers aislados
    private readonly OpenDocumentStore _documentStore;

    public DiagnosticHandler(
        ILogger<DiagnosticHandler> logger,
        IServiceProvider serviceProvider,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public async Task<RelatedDocumentDiagnosticReport> Handle(
        DocumentDiagnosticParams request,
        CancellationToken cancellationToken)
    {
        // Timeout de seguridad: Si tarda más de 2s, abortamos para no colgar el cliente
        using var timeoutCts = new CancellationTokenSource(2000);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var token = linkedCts.Token;

        try
        {
            var uri = request.TextDocument.Uri.ToUri();
            
            // Variables para almacenar lo que encontremos
            string? content = null;
            Mql4File? mql4File = null;

            // 1. INTENTO MEMORIA (Prioritario)
            if (_documentStore.TryGetValue(uri, out var storedFile, out var storedContent))
            {
                mql4File = storedFile;
                content = storedContent;
            }

            // 2. INTENTO DISCO (Fallback si no hay DidOpen previo)
            if (string.IsNullOrEmpty(content))
            {
                var fsPath = request.TextDocument.Uri.GetFileSystemPath();
                if (!string.IsNullOrEmpty(fsPath) && File.Exists(fsPath))
                {
                    try 
                    {
                        content = await File.ReadAllTextAsync(fsPath, token);
                    }
                    catch (IOException) { /* Ignorar si está bloqueado */ }
                }
            }

            // Si no tenemos contenido, no podemos hacer nada. Devolvemos vacío.
            if (string.IsNullOrEmpty(content))
            {
                return CreateEmptyReport();
            }

            // 3. PARSING SEGURO (Thread-Safe)
            // Si no teníamos el modelo parseado, lo creamos ahora usando un parser FRESCO.
            if (mql4File == null)
            {
                // Obtenemos una instancia nueva del parser para evitar conflictos de hilos
                var freshParser = _serviceProvider.GetRequiredService<Mql4AntlrParser>();
                var filePath = request.TextDocument.Uri.GetFileSystemPath() ?? "unknown.mq4";

                // Parseamos en un hilo aparte
                mql4File = await Task.Run(() => freshParser.ParseFile(content!, filePath), token);
                
                // Guardamos en caché
                _documentStore.AddOrUpdate(uri, mql4File, content!);
            }

            // 4. GENERAR REPORTE
            // Aquí usamos 'content!' porque el check de IsNullOrEmpty de arriba nos garantiza que tiene texto.
            return GenerateReport(mql4File, content!, token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Diagnostic request timed out.");
            return CreateEmptyReport();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostic handler failed.");
            return CreateEmptyReport();
        }
    }

    private RelatedFullDocumentDiagnosticReport GenerateReport(Mql4File? file, string content, CancellationToken token)
    {
        var diagnostics = GenerateDiagnostics(file, content, token);
        
        return new RelatedFullDocumentDiagnosticReport
        {
            ResultId = Guid.NewGuid().ToString(),
            Items = diagnostics
        };
    }

    private RelatedFullDocumentDiagnosticReport CreateEmptyReport()
    {
        return new RelatedFullDocumentDiagnosticReport
        {
            ResultId = null,
            Items = Array.Empty<Diagnostic>()
        };
    }

    public DiagnosticsRegistrationOptions GetRegistrationOptions(
        DiagnosticClientCapabilities capability,
        ClientCapabilities clientCapabilities)
    {
        return new DiagnosticsRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector(),
            InterFileDependencies = false,
            WorkspaceDiagnostics = false
        };
    }

    private List<Diagnostic> GenerateDiagnostics(Mql4File? mql4File, string content, CancellationToken token)
    {
        var diagnostics = new List<Diagnostic>();
        
        // Split simple. Para archivos gigantescos sería mejor buscar índices manuales,
        // pero para MQL4 esto es suficiente.
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            // Verificamos cancelación periódicamente
            if (i % 100 == 0) token.ThrowIfCancellationRequested();

            var line = lines[i];

            // 1. Typos
            if (line.Contains("UnkownFunction") || line.Contains("UnkownVariable"))
            {
                diagnostics.Add(new Diagnostic {
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, 0, i, line.Length),
                    Severity = DiagnosticSeverity.Error,
                    Message = "Potential typo: 'Unkown' should be 'Unknown'",
                    Code = "MQL4001",
                    Source = "mql4-lsp"
                });
            }

            // 2. OnInit vacío
            if (line.Contains("int OnInit()") && i + 1 < lines.Length)
            {
                var nextLine = lines[i + 1].Trim();
                if (string.IsNullOrEmpty(nextLine) || nextLine == "{}")
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, 0, i, line.Length),
                        Severity = DiagnosticSeverity.Warning,
                        Message = "OnInit function appears to be empty.",
                        Code = "MQL4002",
                        Source = "mql4-lsp"
                    });
                }
            }
        }
        
        // 3. Símbolos (Underscores)
        if (mql4File?.Symbols != null)
        {
            foreach (var symbol in mql4File.Symbols)
            {
                token.ThrowIfCancellationRequested();

                if (symbol.Name.StartsWith("_") && symbol.Kind == SymbolKind.Variable)
                {
                    diagnostics.Add(new Diagnostic {
                        Range = symbol.Range,
                        Severity = DiagnosticSeverity.Hint,
                        Message = $"Variable '{symbol.Name}' starts with underscore",
                        Code = "MQL4003",
                        Source = "mql4-lsp"
                    });
                }
            }
        }
        return diagnostics;
    }
}