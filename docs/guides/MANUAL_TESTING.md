# Manual Testing Guide - MQL4 Language Server

This guide provides step-by-step instructions to manually test the MQL4 Language Server functionality.

## 📋 Prerequisites

Choose one installation method:

### Option 1: Standalone Binary (Recommended)
```bash
# Download from GitHub Releases
wget https://github.com/davalillo/mql-language-server/releases/latest/download/mql-lsp-server-linux-x64.tar.gz

# Extract and make executable
tar -xzf mql-lsp-server-linux-x64.tar.gz
chmod +x mql-lsp-server

# Test binary
./mql-lsp-server --version
```

### Option 2: From Source Build
```bash
# Build the project
git clone https://github.com/davalillo/mql-language-server.git
cd mql-language-server
./build.sh

# Binary location
./src/bin/linux-x64/publish/mql-lsp-server --version
```

### Option 3: .NET Tool
```bash
# Requires .NET 10 SDK
dotnet tool install --global mql-language-server

# Verify installation
mql-lsp-server --version
```

## 🧪 Testing Setup

### 1. Create Test MQL4 File

Create `test.mq4` with the following code:

```mql4
//+------------------------------------------------------------------+
//|                                                   test-strategy.mq4 |
//|                                  MQL4 Language Server Test File |
//+------------------------------------------------------------------+
#property copyright "Test"
#property link      ""
#property version   "1.00"
#property strict

#include <Trade/Trade.mqh>

// Input parameters
input int      InpMagicNumber = 12345;
input double   InpLotSize     = 0.1;

// Global variables
int magic = 12345;
double lotSize = 0.1;

// Custom functions
void Initialize() {
    Print("Strategy initialized");
}

double CalculatePositionSize(double risk) {
    double balance = AccountInfoDouble(ACCOUNT_BALANCE);
    double size = balance * risk / 100;
    return NormalizeDouble(size / 10000, 2);
}

// Built-in MQL4 functions
int OnInit() {
    Print("OnInit called");
    Initialize();
    return(INIT_SUCCEEDED);
}

void OnTick() {
    // Get market data
    double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
    double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);

    // Calculate indicator
    double ma = iMA(_Symbol, PERIOD_H1, 20, 0, MODE_SMA, PRICE_CLOSE, 1);

    // Trading logic
    if (ask > ma && PositionsTotal() == 0) {
        CTrade trade;
        double lots = CalculatePositionSize(2.0);
        trade.Buy(lots, _Symbol, ask, 0, ask + 100 * _Point, "Test");
    }
}

void OnDeinit(const int reason) {
    Print("OnDeinit called, reason: ", reason);
}
```

### 2. Test with stdio (LSP Protocol)

```bash
# Create LSP request file
cat > test-request.json << 'EOF'
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "processId": null,
    "rootUri": "file:///tmp/mql4-test",
    "capabilities": {}
  }
}
EOF

# Send request to LSP server
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"rootUri":"file:///tmp/mql4-test","capabilities":{}}}' | ./mql-lsp-server --stdio

# Expected: JSON-RPC response with server capabilities
```

### 3. Test with VSCode

#### Install VSCode Extension Configuration

Create `.vscode/settings.json` in your MQL4 project:

```json
{
  "languageServers": {
    "MQL4": {
      "command": "./mql-lsp-server",
      "args": ["--stdio"],
      "languages": ["mql4"],
      " filetypes": ["mq4", "mqh"]
    }
  },
  "files.associations": {
    "*.mq4": "mql4",
    "*.mqh": "mql4"
  }
}
```

#### Test Steps in VSCode

1. **Open `test.mq4`** in VSCode
2. **Verify LSP features:**

   **Document Symbols (Outline)**
   - Press `Ctrl+Shift+O` (Windows/Linux) or `Cmd+Shift+O` (macOS)
   - Expected: Outline showing functions (OnInit, OnTick, OnDeinit), variables (InpMagicNumber, InpLotSize), custom functions (Initialize, CalculatePositionSize)

   **Go to Definition**
   - Right-click `CalculatePositionSize` → "Go to Definition"
   - OR: Hold `Ctrl` (Windows/Linux) or `Cmd` (macOS) and click the function name
   - Expected: Cursor jumps to line 23

   **Find All References**
   - Right-click `InpMagicNumber` → "Find All References"
   - OR: Press `Shift+F12`
   - Expected: Shows all usages of InpMagicNumber

   **Hover**
   - Hover over `AccountInfoDouble` function
   - Expected: Tooltip showing function signature and description

   **Completion**
   - Type `Acco` and trigger autocomplete (`Ctrl+Space`)
   - Expected: Shows `AccountInfoDouble`, `AccountBalance`, etc.
   - Type `SymbolInfoDouble` and trigger autocomplete
   - Expected: Shows parameter hints `SYMBOL_ASK`, `SYMBOL_BID`, `SYMBOL_POINT`, etc.

### 4. Test with Neovim

#### Configuration (init.lua)

```lua
-- Install nvim-lspconfig
require('lspconfig').mql4_lsp = {
    cmd = {'./mql-lsp-server', '--stdio'},
    filetypes = {'mql4'},
}

-- Keybindings
vim.api.nvim_set_keymap('n', 'gd', '<cmd>lua vim.lsp.buf.definition()<CR>', {noremap = true})
vim.api.nvim_set_keymap('n', 'gr', '<cmd>lua vim.lsp.buf.references()<CR>', {noremap = true})
vim.apinvim_set_keymap('n', 'K', '<cmd>lua vim.lsp.buf.hover()<CR>', {noremap = true})
```

#### Test Commands

```bash
# Open file in Neovim
nvim test.mq4

# Test LSP commands:
# :LspInfo                    - Check LSP status
# gd                         - Go to definition
# gr                         - Find references
# K                          - Hover info
# :LspDocumentSymbol         - Document symbols
```

### 5. Test Specific Features

#### A. Symbol Extraction Test

```bash
# Count parsed symbols
./mql-lsp-server --stdio <<EOF
{"jsonrpc":"2.0","id":1,"method":"textDocument/documentSymbol","params":{"textDocument":{"uri":"file:///tmp/test.mq4"}}}
EOF

# Expected: JSON with 13 symbols (OnInit, OnTick, OnDeinit, Initialize, CalculatePositionSize, InpMagicNumber, InpLotSize, magic, lotSize, etc.)
```

#### B. Completion Test

```bash
# Test keyword completion
./mql-lsp-server --stdio <<EOF
{"jsonrpc":"2.0","id":1,"method":"textDocument/completion","params":{"textDocument":{"uri":"file:///tmp/test.mq4"},"position":{"line":0,"character":0}}}
EOF

# Expected: Keywords like "if", "while", "for", "double", "int", etc.
```

#### C. Built-in Functions Test

```bash
# Test MQL4 built-ins completion
./mql-lsp-server --stdio <<EOF
{"jsonrpc":"2.0","id":1,"method":"textDocument/completion","params":{"textDocument":{"uri":"file:///tmp/test.mq4"},"position":{"line":30,"character":5}}}
EOF

# Expected: AccountInfoDouble, SymbolInfoDouble, iMA, PositionsTotal, etc.
```

## 🔍 Troubleshooting

### LSP Server Not Starting

```bash
# Check if binary is executable
./mql-lsp-server --help

# Check stdio mode
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"rootUri":"file:///tmp","capabilities":{}}}' | ./mql-lsp-server --stdio
```

### No Completion Suggestions

1. Verify file is associated with MQL4 language
2. Check LSP server is running: `ps aux | grep mql-lsp-server`
3. Check LSP logs in editor's output panel

### Parse Errors

MQL4 parser is ANTLR-based. If parsing fails:
- Check syntax: Missing semicolons, braces
- Verify `#include` paths are correct
- Restart LSP server

### Feature Not Working

1. **Check Language ID**: Ensure file has `.mq4` or `.mqh` extension
2. **Restart LSP**: Close and reopen file
3. **Check Capabilities**: Run with `--verbose` flag

## 📊 Expected Test Results

| Feature | Test File Line | Expected Result |
|---------|---------------|-----------------|
| Document Symbols | N/A | 13 symbols in outline |
| Go to Definition | `CalculatePositionSize` (line 23) | Cursor jumps to line 23 |
| Find References | `InpMagicNumber` | Shows 2 references (declaration + usage) |
| Hover | `AccountInfoDouble` | Tooltip with function signature |
| Completion | Type `Acco` | Shows 8-10 built-in functions |
| Variable Completion | Inside function | Shows local and global variables |

## 🎯 Performance Test

```bash
# Test with larger file (1000+ lines)
time ./mql-lsp-server --stdio < large-test-request.json

# Expected: < 500ms for initialization
# Expected: < 100ms for completion
# Expected: < 200ms for document symbols
```

## 📝 Test Checklist

- [ ] Binary executable and runs
- [ ] Stdio communication works
- [ ] VSCode integration configured
- [ ] Neovim integration configured
- [ ] Document symbols extracted (13+ symbols)
- [ ] Go to definition works
- [ ] Find references works
- [ ] Hover displays information
- [ ] Completion shows MQL4 keywords
- [ ] Completion shows built-in functions
- [ ] Custom variables appear in completion
- [ ] Parse error handling works
- [ ] Performance acceptable (< 500ms)

## 🐛 Common Issues

### Issue: "LSP server failed to start"

**Solution**: Check binary path in editor configuration

### Issue: "No completion suggestions"

**Solution**: Verify file extension is `.mq4` or language is set to `mql4`

### Issue: "Parse error on valid MQL4 code"

**Solution**: Parser supports simplified MQL4 grammar. See ANTLR grammar limitations.

### Issue: "Commands not responding"

**Solution**: Check LSP server is running: `ps aux | grep mql-lsp-server`

---

**Test File Location**: `/tmp/test.mq4` (as referenced in examples)

**Binary Location**: `./mql-lsp-server` (adjust path as needed)
