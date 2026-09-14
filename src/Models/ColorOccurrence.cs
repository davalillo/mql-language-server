namespace MqlLanguageServer.Models;

/// <summary>
/// Original source form of a captured color occurrence (D2): the kind preserves
/// how the color was written so colorPresentation can label edits appropriately.
/// </summary>
public enum ColorKind
{
    /// <summary>MQL color literal, e.g. C'255,0,0' (REQ-CP-01).</summary>
    CLiteral,

    /// <summary>Hex integer 0xRRGGBB or 0xAARRGGBB (REQ-CP-02).</summary>
    Hex,

    /// <summary>Named clr* constant resolved via MqlColorRegistry (REQ-CP-03).</summary>
    Named
}

/// <summary>
/// A single color literal captured at parse time (D2, REQ-CP-01..03).
/// Byte channels keep parse-side math integer and language-agnostic; the
/// ÷255 float scaling to LSP lives only in ColorPresentationService.
/// Line/Column are 0-based (mirrors TokenOccurrence); Alpha is 255 for
/// every form except 0xAARRGGBB hex (REQ-CP-02). Name is populated only
/// for <see cref="ColorKind.Named"/> occurrences.
/// </summary>
public sealed record ColorOccurrence(
    int Line,
    int Column,
    int Length,
    ColorKind Kind,
    byte R,
    byte G,
    byte B,
    byte Alpha,
    string? Name);