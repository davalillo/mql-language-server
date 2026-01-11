using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using Mql4LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handles workspace/symbol requests for searching symbols across all files in the workspace.
/// </summary>
public class WorkspaceSymbolHandler : OmniSharp.Extensions.LanguageServer.Protocol.Workspace.IWorkspaceSymbolsHandler
{
    private readonly ILogger<WorkspaceSymbolHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly GlobalSymbolIndex _globalSymbolIndex;

    public WorkspaceSymbolHandler(
        ILogger<WorkspaceSymbolHandler> logger,
        Mql4AntlrParser parser,
        GlobalSymbolIndex globalSymbolIndex)
    {
        _logger = logger;
        _parser = parser;
        _globalSymbolIndex = globalSymbolIndex;
    }

    public WorkspaceSymbolRegistrationOptions GetRegistrationOptions(WorkspaceSymbolCapability capability, ClientCapabilities clientCapabilities)
    {
        throw new NotImplementedException();
    }

    public Task<Container<WorkspaceSymbol>?> Handle(WorkspaceSymbolParams request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
