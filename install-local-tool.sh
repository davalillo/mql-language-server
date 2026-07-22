#!/bin/bash

# =============================================================================
# MQL Language Server - Local Installation Script
# =============================================================================
# This script demonstrates how to install MQL LSP as a .NET global tool
# WITHOUT publishing to nuget.org
# =============================================================================

set -e

echo "╔══════════════════════════════════════════════════════════════════════════╗"
echo "║          MQL Language Server - Local .NET Tool Installation             ║"
echo "╚══════════════════════════════════════════════════════════════════════════╝"
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Function to print colored output
print_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
print_step "Checking prerequisites..."
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK is not installed!"
    echo "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_success ".NET SDK version: $DOTNET_VERSION"

# Check if we're in the right directory
if [ ! -f "MqlLanguageServer.sln" ]; then
    print_error "MqlLanguageServer.sln not found!"
    echo "Please run this script from the mql-language-server repository root"
    exit 1
fi

print_success "Found project files"
echo ""

# Step 1: Build the project
print_step "Building the project..."
dotnet build -c Release
print_success "Build completed"
echo ""

# Step 2: Create NuGet package
print_step "Creating NuGet package..."
PACKAGE_DIR="./nupkg-local"
rm -rf "$PACKAGE_DIR"
dotnet pack -c Release -o "$PACKAGE_DIR" --include-symbols

# Find the created .nupkg file
NUPKG_FILE=$(find "$PACKAGE_DIR" -name "*.nupkg" | head -n 1)

if [ -z "$NUPKG_FILE" ]; then
    print_error "Failed to create NuGet package"
    exit 1
fi

print_success "NuGet package created: $NUPKG_FILE"
echo ""

# Step 3: Install as global tool from local source
print_step "Installing MQL Language Server as global tool..."
dotnet tool install --global mql-language-server \
    --version 1.0.0 \
    --add-source "$PACKAGE_DIR"

print_success "Tool installed successfully"
echo ""

# Step 4: Verify installation
print_step "Verifying installation..."
TOOL_PATH=$(which mql-lsp-server || echo "Tool not found in PATH")

if [ -z "$TOOL_PATH" ]; then
    print_warning "Tool might be installed but not in PATH"
    print_warning "Try: export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
    echo ""
    print_step "Checking tool manifest..."
    dotnet tool list -g
else
    print_success "Tool location: $TOOL_PATH"
fi

echo ""

# Step 5: Test the tool
print_step "Testing the tool..."
echo "Testing: mql-lsp-server --version"
timeout 5 mql-lsp-server --version 2>&1 || echo "(version command not implemented)"

echo ""
print_success "mql-lsp-server tool is ready to use!"
echo ""

# Step 6: Provide usage instructions
echo "╔══════════════════════════════════════════════════════════════════════════╗"
echo "║                           Installation Complete!                         ║"
echo "╚══════════════════════════════════════════════════════════════════════════╝"
echo ""
echo "📦 Package Location: $NUPKG_FILE"
echo "🔧 Tool Command: mql-lsp-server"
echo "📝 Arguments: --stdio"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📋 Next Steps:"
echo ""
echo "1. Configure your editor to use the LSP server:"
echo ""
echo "   VSCode (settings.json):"
echo '   {'
echo '     "languageServers": {'
echo '       "MQL4": {'
echo '         "command": "mql-lsp-server",'
echo '         "args": ["--stdio"]'
echo '       }'
echo '     }'
echo '   }'
echo ""
echo "2. Open a .mq4 file in your editor"
echo "3. Type 'OnInit' to test auto-completion"
echo "4. Hover over built-in functions (Ask, Bid) to see info"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "💡 To uninstall:"
echo "   dotnet tool uninstall --global mql-language-server"
echo ""
echo "💡 To reinstall:"
echo "   Run this script again"
echo ""
