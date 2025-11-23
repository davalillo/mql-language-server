#!/bin/bash
# Script to verify ANTLR files are being regenerated
# Run this before and after a clean build to confirm regeneration

echo "=================================================="
echo "ANTLR File Regeneration Verification Script"
echo "=================================================="
echo ""

# Check if we're in the right directory
if [ ! -f "src/Mql4LanguageServer.Server.csproj" ]; then
    echo "❌ Error: Run this script from the project root directory"
    exit 1
fi

echo "📋 Current ANTLR generated files in src/Parser/Generated/:"
echo ""

# List all ANTLR generated files with timestamps
if [ -d "src/Parser/Generated" ]; then
    ls -lah src/Parser/Generated/*.{cs,interp,tokens} 2>/dev/null | head -20
else
    echo "   (Directory doesn't exist yet - run build.sh first)"
fi

echo ""
echo "📋 Grammar file timestamp:"
if [ -f "src/Mql4/Grammar/Mql4Grammar.g4" ]; then
    ls -lah src/Mql4/Grammar/Mql4Grammar.g4
else
    echo "   ❌ Grammar file not found!"
fi

echo ""
echo "📋 ANTLR generated files count:"
if [ -d "src/Parser/Generated" ]; then
    echo "   *.cs files: $(ls src/Parser/Generated/*.cs 2>/dev/null | wc -l)"
    echo "   *.interp files: $(ls src/Parser/Generated/*.interp 2>/dev/null | wc -l)"
    echo "   *.tokens files: $(ls src/Parser/Generated/*.tokens 2>/dev/null | wc -l)"
else
    echo "   (No generated files directory yet)"
fi

echo ""
echo "=================================================="
echo "🔍 To verify regeneration, run this script:"
echo "   1. Before build.sh"
echo "   2. After build.sh"
echo "   The file timestamps should be newer after build"
echo "=================================================="
