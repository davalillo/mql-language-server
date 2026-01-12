using System;
using System.Linq;
using System.Reflection;

// Check the actual signature needed
var diagHandlerInterface = typeof(OmniSharp.Extensions.LanguageServer.Protocol.Document.IDocumentDiagnosticHandler);
Console.WriteLine($"IDocumentDiagnosticHandler interface details:");
var registrationInterface = diagHandlerInterface.GetInterface("IRegistration`2");
if (registrationInterface != null) {
    Console.WriteLine($"  Implements IRegistration`2");
    var genericArgs = registrationInterface.GetGenericArguments();
    Console.WriteLine($"    TOptions: {genericArgs[0].FullName}");
    Console.WriteLine($"    TCapability: {genericArgs[1].FullName}");
}

// Check what DiagnosticClientCapabilities looks like
var diagCapType = Type.GetType("OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticClientCapabilities, OmniSharp.Extensions.LanguageProtocol");
if (diagCapType != null) {
    Console.WriteLine($"\nDiagnosticClientCapabilities found!");
    Console.WriteLine($"  Properties:");
    foreach (var pi in diagCapType.GetProperties()) {
        Console.WriteLine($"    {pi.PropertyType.Name} {pi.Name}");
    }
}

// Check DiagnosticsRegistrationOptions
var regOptsType = Type.GetType("OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticsRegistrationOptions, OmniSharp.Extensions.LanguageProtocol");
if (regOptsType != null) {
    Console.WriteLine($"\nDiagnosticsRegistrationOptions found!");
    Console.WriteLine($"  Properties:");
    foreach (var pi in regOptsType.GetProperties()) {
        Console.WriteLine($"    {pi.PropertyType.Name} {pi.Name}");
    }
}
