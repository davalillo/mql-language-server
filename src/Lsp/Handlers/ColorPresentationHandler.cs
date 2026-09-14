using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Color;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Container = OmniSharp.Extensions.LanguageServer.Protocol.Models.Container<
    OmniSharp.Extensions.LanguageServer.Protocol.Models.ColorPresentation>;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for textDocument/colorPresentation (REQ-CP-06, REQ-CP-07; D4).
/// Implements <c>IColorPresentationHandler</c>, which does NOT participate in
/// registration (OmniSharp: IDoesNotParticipateInRegistration) — no
/// GetRegistrationOptions; the colorProvider capability derives solely from
/// <see cref="DocumentColorHandler"/>. Presentations come from
/// <see cref="ColorPresentationService.CreatePresentations"/> (three forms,
/// edits at the request range).
/// </summary>
public class ColorPresentationHandler
    : LanguageAwareHandlerBase<ColorPresentationParams, Container>, IColorPresentationHandler
{
    private readonly ILogger<ColorPresentationHandler> _logger;

    public ColorPresentationHandler(
        ILogger<ColorPresentationHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Container> Handle(ColorPresentationParams request, CancellationToken cancellationToken)
    {
        var language = ResolveLanguage(request.TextDocument.Uri.ToUri());
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    public void SetCapability(ColorProviderCapability capability, ClientCapabilities clientCapabilities)
    {
        // IColorPresentationHandler implements ICapability<ColorProviderCapability>
        // but NOT IRegistration — capability advertisement derives solely from
        // DocumentColorHandler's registration options (D4). Nothing to set here.
    }

    protected override Container HandleForLanguage(
        ColorPresentationParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            // Pure conversion: the request already carries the color floats and
            // the target range; the document is not needed (REQ-CP-06).
            var presentations = ColorPresentationService.CreatePresentations(request.Color, request.Range);
            return new Container(presentations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ColorPresentation handler failed.");
            return new Container(System.Linq.Enumerable.Empty<ColorPresentation>());
        }
    }
}