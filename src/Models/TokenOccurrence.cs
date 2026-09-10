namespace MqlLanguageServer.Models;

/// <summary>
/// A single identifier token occurrence captured during parsing
/// (OCC-01). Line and Column are 0-based; Length is the token text length.
/// Immutable positional value; one per identifier token per parse.
/// </summary>
public sealed record TokenOccurrence(string Text, int Line, int Column, int Length);