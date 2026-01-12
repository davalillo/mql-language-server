using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didChange text document notification
/// </summary>
public class DidChangeTextDocumentHandler : IDidChangeTextDocumentHandler
{
    private readonly ILogger<DidChangeTextDocumentHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _openFiles;
    // Añadimos GlobalSymbolIndex para mantener el workspace actualizado en tiempo real
    private readonly GlobalSymbolIndex _globalSymbolIndex; 

    public DidChangeTextDocumentHandler(
        ILogger<DidChangeTextDocumentHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore openFiles,
        GlobalSymbolIndex globalSymbolIndex)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _openFiles = openFiles ?? throw new ArgumentNullException(nameof(openFiles));
        _globalSymbolIndex = globalSymbolIndex ?? throw new ArgumentNullException(nameof(globalSymbolIndex));
        
        _logger.LogInformation("DidChangeTextDocumentHandler initialized");
    }

    public TextDocumentChangeRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentChangeRegistrationOptions
        {
            // CRUCIAL: Pedimos al cliente que envíe el texto COMPLETO en cada cambio.
            // Esto evita tener que implementar algoritmos complejos de parcheo de strings.
            SyncKind = TextDocumentSyncKind.Full,
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }

    public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri.ToUri();
            var changes = request.ContentChanges;

            _logger.LogDebug("Processing document changes for: {DocumentUri}", documentUri);

            // Obtenemos el nuevo contenido completo
            // Al usar SyncKind.Full, el último cambio contiene todo el texto del archivo.
            var newContent = GetFullContent(changes);

            if (!string.IsNullOrEmpty(newContent))
            {
                var filePath = documentUri.AbsolutePath ?? "unknown";

                // 1. Reparsear el archivo con el nuevo contenido
                var newMql4File = _parser.ParseFile(newContent, filePath);

                // 2. Actualizar el OpenDocumentStore con el MODELO y el TEXTO CRUDO
                // Esto es vital para que DiagnosticHandler no tenga que leer del disco
                _openFiles.AddOrUpdate(documentUri, newMql4File, newContent);

                // 3. Actualizar el índice global para búsquedas de Workspace
                _globalSymbolIndex.AddFile(filePath, newMql4File.Symbols);

                // 4. (Opcional) Actualizar dependencias si han cambiado los #include
                UpdateIncludes(newMql4File, filePath);

                _logger.LogDebug("Re-parsed {SymbolCount} symbols after document change", newMql4File.Symbols.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling didChange for {Uri}", request.TextDocument.Uri);
        }

        return Task.FromResult(Unit.Value);
    }

    private string GetFullContent(Container<TextDocumentContentChangeEvent> changes)
    {
        // En modo Full Sync, nos interesa el último evento que tiene todo el texto.
        var changeList = changes.ToArray();
        if (changeList.Length > 0)
        {
            var lastChange = changeList[changeList.Length - 1];
            if (lastChange.Text != null)
            {
                return lastChange.Text;
            }
        }
        return string.Empty;
    }

    private void UpdateIncludes(Mql4File file, string filePath)
    {
        // Limpiamos dependencias antiguas si es necesario (depende de tu implementación de GlobalSymbolIndex)
        // Y registramos las nuevas
        foreach (var include in file.Includes)
        {
            // Lógica simplificada de includes, similar a DidOpen
            var includePath = ExtractIncludePath(include);
            if (!string.IsNullOrEmpty(includePath))
            {
                // Aquí podrías añadir lógica para resolver la ruta completa
                // _globalSymbolIndex.AddDependency(filePath, resolvedPath);
            }
        }
    }

    private string ExtractIncludePath(string includeDirective)
    {
        var match = System.Text.RegularExpressions.Regex.Match(includeDirective, @"#include\s+""([^""]+)""");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}