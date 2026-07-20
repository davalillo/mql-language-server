using System;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

public class LanguageDetectionTests
{
    [Fact]
    public void Detect_ClientLanguageId_Mql5_Returns_Mql5()
    {
        var uri = new Uri("file:///x.mq5");
        var result = LanguageDetection.Detect(uri, "mql5", "int start() { return 0; }");
        Assert.Equal(MqlLanguage.Mql5, result);
    }

    [Fact]
    public void Detect_ClientLanguageId_Mql4_Returns_Mql4()
    {
        var uri = new Uri("file:///x.mq4");
        var result = LanguageDetection.Detect(uri, "mql4", "int start() { return 0; }");
        Assert.Equal(MqlLanguage.Mql4, result);
    }

    [Fact]
    public void Detect_UnknownLanguageId_Returns_Mql4()
    {
        var uri = new Uri("file:///x.py");
        var result = LanguageDetection.Detect(uri, "python", "print('hello')");
        Assert.Equal(MqlLanguage.Mql4, result);
    }

    [Fact]
    public void Detect_Mqh_With_Mql5Token_Point_Returns_Mql5()
    {
        var uri = new Uri("file:///x.mqh");
        var result = LanguageDetection.Detect(uri, null, "void OnTick() { double p = _Point; }");
        Assert.Equal(MqlLanguage.Mql5, result);
    }

    [Fact]
    public void Detect_Mqh_Only_Mql4_Returns_Mql4()
    {
        var uri = new Uri("file:///x.mqh");
        var result = LanguageDetection.Detect(uri, null, "int start() { return(0); }");
        Assert.Equal(MqlLanguage.Mql4, result);
    }

    [Fact]
    public void Detect_Mqh_Empty_Returns_Mql4()
    {
        var uri = new Uri("file:///x.mqh");
        var result = LanguageDetection.Detect(uri, null, "");
        Assert.Equal(MqlLanguage.Mql4, result);
    }

    [Fact]
    public void Detect_Mqh_Mql5Token_After_600_Bytes_Returns_Mql5()
    {
        var uri = new Uri("file:///x.mqh");
        var prefix = new string('x', 600);
        var result = LanguageDetection.Detect(uri, null, $"{prefix}\nvoid f() {{ int* p = nullptr; }}");
        Assert.Equal(MqlLanguage.Mql5, result);
    }

    [Fact]
    public void Detect_Mqh_With_Using_System_Returns_Mql4()
    {
        var uri = new Uri("file:///x.mqh");
        var result = LanguageDetection.Detect(uri, null, "using System;");
        Assert.Equal(MqlLanguage.Mql4, result);
    }
}
