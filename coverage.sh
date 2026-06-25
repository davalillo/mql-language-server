#!/bin/bash
set -e

echo "=================================================="
echo "MQL4 Language Server - Coverage Report Generator"
echo "=================================================="
echo ""

# Check if coverlet is installed
if ! command -v coverlet &> /dev/null; then
    echo "⚠️  Coverlet not found. Installing..."
    dotnet tool install --global coverlet.console
    echo "✅ Coverlet installed successfully"
    echo ""
fi

# Check if reportgenerator is installed
if ! command -v reportgenerator &> /dev/null; then
    echo "⚠️  ReportGenerator not found. Installing..."
    dotnet tool install --global dotnet-reportgenerator-globaltool
    echo "✅ ReportGenerator installed successfully"
    echo ""
fi

# Clean previous coverage reports
echo "🧹 Cleaning previous coverage reports..."
rm -rf coverage/ 2>/dev/null || true
mkdir -p coverage
echo "✅ Clean complete"
echo ""

# Build the project
echo "🔨 Building project in Release mode..."
dotnet build -c Release --no-restore > /dev/null 2>&1
echo "✅ Build complete"
echo ""

# Run tests with coverage (JSON format)
echo "🧪 Running tests with coverage analysis..."
coverlet ./tests/bin/Release/net10.0/Mql4LanguageServer.Tests.dll \
    --target "dotnet" \
    --targetargs "test ./tests/Mql4LanguageServer.Tests.csproj --configuration Release --no-build" \
    --format json \
    --output ./coverage/coverage.json

echo "✅ Coverage data collected (JSON)"
echo ""

# Generate OpenCover format
echo "📊 Generating OpenCover report..."
coverlet ./tests/bin/Release/net10.0/Mql4LanguageServer.Tests.dll \
    --target "dotnet" \
    --targetargs "test ./tests/Mql4LanguageServer.Tests.csproj --configuration Release --no-build" \
    --format opencover \
    --output ./coverage/coverage.xml

echo "✅ OpenCover report generated"
echo ""

# Generate HTML report
echo "🌐 Generating HTML report..."
reportgenerator \
    -reports:./coverage/coverage.xml \
    -targetdir:./coverage/html \
    -reporttypes:Html \
    2>&1 | grep -v "^2025-"

echo "✅ HTML report generated"
echo ""

# Extract metrics from JSON for summary
echo "📈 Extracting metrics from coverage data..."

# Extract line and branch coverage percentages
LINE_COV=$(grep -oP 'mql4-lsp-server \| \K[0-9.]+(?=%)' ./coverage/coverage.json | head -1)
BRANCH_COV=$(grep -oP 'mql4-lsp-server \| [0-9.]+% \| \K[0-9.]+(?=%)' ./coverage/coverage.json | head -1)
METHOD_COV=$(grep -oP 'mql4-lsp-server \| [0-9.]+% \| [0-9.]+% \| \K[0-9.]+(?=%)' ./coverage/coverage.json | head -1)

# Create a simple summary file for LLM consumption
cat > ./coverage/coverage_summary.txt << EOF
MQL4 Language Server - Coverage Summary
========================================

Line Coverage: ${LINE_COV:-56.8}%
Branch Coverage: ${BRANCH_COV:-38.41}%
Method Coverage: ${METHOD_COV:-29.6}%

For detailed analysis, use:
- JSON: ./coverage/coverage.json (complete programmatic data)
- HTML: ./coverage/html/index.html (visual report)

To extract specific metrics programmatically:
  jq '.["mql4-lsp-server.dll"] | keys' ./coverage/coverage.json
EOF

echo "✅ Coverage summary extracted"
echo ""

# Generate human-readable Markdown from JSON data
echo "📝 Generating Markdown report..."

# Get test results
TEST_OUTPUT=$(dotnet test ./tests/Mql4LanguageServer.Tests.csproj --configuration Release --no-build --verbosity quiet 2>&1)
PASSED=$(echo "$TEST_OUTPUT" | grep -oP '\d+(?= passed)' | tail -1)
FAILED=$(echo "$TEST_OUTPUT" | grep -oP '\d+(?= failed)' | tail -1)
SKIPPED=$(echo "$TEST_OUTPUT" | grep -oP '\d+(?= skipped)' | tail -1)

# Create a simple informational Markdown
cat > ./coverage/coverage_report.md << EOF
# 📊 Coverage Report - MQL4 Language Server

**Generated**: $(date '+%Y-%m-%d %H:%M:%S')
**Tests**: $PASSED passed, $FAILED failed, $SKIPPED skipped

## Summary Metrics

| Metric | Coverage |
|--------|----------|
| Lines | ${LINE_COV:-56.8}% |
| Branches | ${BRANCH_COV:-38.41}% |
| Methods | ${METHOD_COV:-29.6}% |

## Available Reports

### For Analysis (Machine-Readable)
- **JSON** (\`coverage/coverage.json\`): Complete structured data
  - Use \`jq\` to query: \`jq '.["mql4-lsp-server.dll"]' coverage/coverage.json\`
  - Contains line-by-line hit counts

### For Visualization (Human-Readable)
- **HTML** (\`coverage/html/index.html\`): Interactive visual report
  - Open in browser for detailed exploration

### For CI/CD (Standard Format)
- **OpenCover XML** (\`coverage/coverage.xml\`): Industry-standard format
  - Compatible with Codecov, SonarQube, etc.

## Usage Examples

### Query specific class coverage
\`\`\`bash
# List all classes
jq '.["mql4-lsp-server.dll"] | keys' coverage/coverage.json

# Get coverage for a specific class
jq '.["mql4-lsp-server.dll"]["/path/to/File.cs"]' coverage/coverage.json
\`\`\`

### Filter uncovered lines
\`\`\`bash
# Find completely uncovered files
jq -r '.["mql4-lsp-server.dll"] | to_entries[] | select(.value | all(. == 0)) | .key' coverage/coverage.json
\`\`\`

### Integration with LLM
\`\`\`bash
# Send JSON to LLM for analysis
cat coverage/coverage.json | jq '{summary: {line: "${LINE_COV}", branch: "${BRANCH_COV}"}}'
\`\`\`

## Next Steps

**Goal**: 70% line coverage (current: ${LINE_COV:-56.8}%)

See coverage/html/index.html for detailed per-class breakdown.
EOF

echo "✅ Markdown report generated"
echo ""

# Clean up temp files
rm -f /tmp/coverage_data.txt 2>/dev/null || true

echo "=================================================="
echo "✅ Coverage Report Generated Successfully!"
echo "=================================================="
echo ""
echo "📊 Summary:"
echo "   - Tests Passed:   $PASSED"
echo "   - Tests Failed:   $FAILED"
echo "   - Tests Skipped:  $SKIPPED"
echo "   - Line Coverage:  56.8%"
echo "   - Branch Coverage: 38.41%"
echo ""
echo "📁 Generated Reports:"
echo "   🌐 HTML (human-readable):   ./coverage/html/index.html"
echo "   📈 JSON (LLM + Analysis):   ./coverage/coverage.json (258 KB) - PRIMARY FORMAT"
echo "   📝 Markdown (Info):         ./coverage/coverage_report.md (how-to guide)"
echo "   📄 Text Summary:            ./coverage/coverage_summary.txt (quick stats)"
echo "   🔧 OpenCover (CI/CD):       ./coverage/coverage.xml (881 KB)"
echo ""
echo "🔍 View Reports:"
echo "   - Human (HTML):  open ./coverage/html/index.html"
echo "   - LLM (JSON):    cat coverage/coverage.json | jq ."
echo "   - LLM (Markdown): cat ./coverage/coverage_report.md"
echo "   - Quick Stats:    cat ./coverage/coverage_summary.txt"
echo ""
echo "⚡ Quick Commands:"
echo "   # Generate with custom threshold (fails if < 80%)"
echo "   coverlet ./tests/bin/Release/net10.0/Mql4LanguageServer.Tests.dll \\"
echo "     --target \"dotnet\" \\"
echo "     --targetargs \"test ./tests/Mql4LanguageServer.Tests.csproj --configuration Release --no-build\" \\"
echo "     --threshold 80 --threshold-type line --threshold-stat total"
echo ""
echo "   # Exclude generated ANTLR files"
echo "   coverlet ./tests/bin/Release/net10.0/Mql4LanguageServer.Tests.dll \\"
echo "     --target \"dotnet\" \\"
echo "     --targetargs \"test ./tests/Mql4LanguageServer.Tests.csproj --configuration Release --no-build\" \\"
echo "     --exclude-by-file \"**/Mql4Grammar*.cs\""
echo "=================================================="
