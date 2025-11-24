# Benchmark Tool - MQL4 Language Server

## 📋 Overview

The `run-benchmark.sh` script provides a convenient way to run performance benchmarks and track improvements over time.

## 🚀 Quick Start

### Basic Usage

```bash
# Run a benchmark and save as 'phase1'
./run-benchmark.sh phase1

# The benchmark will be saved to: benchmarks/phase1.json
```

### Advanced Usage

```bash
# Run benchmark without comparison
./run-benchmark.sh after-optimization --no-compare

# Force overwrite existing milestone
./run-benchmark.sh phase1 --force

# Show help
./run-benchmark.sh --help
```

## 📊 Workflow Examples

### Before/After Optimization

```bash
# 1. Run baseline (already done)
./run-benchmark.sh baseline-initial

# 2. Make code changes (optimize CompletionHandler, etc.)

# 3. Run benchmark after changes
./run-benchmark.sh after-completion-optimization

# The script automatically compares with baseline-initial.json
# and shows improvement percentage
```

### Multiple Iterations

```bash
# Run multiple iterations to test consistency
./run-benchmark.sh test-iteration-1
./run-benchmark.sh test-iteration-2
./run-benchmark.sh test-iteration-3

# Compare results
diff benchmarks/test-iteration-1.json benchmarks/test-iteration-2.json
```

### Named Milestones

```bash
# Use descriptive names
./run-benchmark.sh before-cache-optimization
# ... make changes ...
./run-benchmark.sh after-cache-optimization

./run-benchmark.sh before-cross-file
# ... make changes ...
./run-benchmark.sh after-cross-file
```

## 🎯 Command Options

| Option | Description |
|--------|-------------|
| `<milestone>` | Name for the benchmark (required) |
| `--compare` | Show comparison vs baseline-initial.json (default) |
| `--no-compare` | Skip comparison |
| `--force` | Overwrite existing milestone without asking |
| `--help` | Show help message |

## 📁 File Structure

```
project-root/
├── run-benchmark.sh          # Benchmark script
├── benchmarks/               # Benchmark results directory
│   ├── baseline-initial.json # Initial baseline (before optimizations)
│   ├── phase1.json          # First milestone
│   ├── phase2.json          # Second milestone
│   └── backups/             # Automatic backups of overwritten files
│       ├── phase1-backup1.json
│       ├── phase1-backup2.json
│       └── ...
└── tests/Performance/BaselineBenchmark.cs # Benchmark implementation
```

## 📊 Understanding Results

### JSON Output Format

```json
{
  "Environment": {
    "PowerMode": "PluggedIn",
    "OS": "Debian GNU/Linux 13 (trixie) X64",
    "DotnetVersion": "10.0.0",
    "ProcessorCount": 32,
    "MachineName": "flanagan",
    "Timestamp": "2025-11-24T18:27:31.3043634Z"
  },
  "Parsing": {
    "Median": 84.18,
    "Average": 149.81,
    "Min": 75.68,
    "Max": 404.97,
    "Samples": 10
  },
  "SymbolCount": {
    "Median": 1937,
    "Range": 1937 - 1937
  },
  "CompletionCount": {
    "Median": 1679,
    "Range": 1679 - 1679
  },
  "CommitHash": "c6efab9"
}
```

### Key Metrics

- **Parsing.Median**: Time to parse the test file (lower is better)
- **SymbolCount.Median**: Number of symbols parsed (should be consistent)
- **CompletionCount.Median**: Total completions available (should be consistent)
- **Samples**: Number of iterations (10 by default)

## 🔍 Comparison Output

When comparing benchmarks, you'll see:

```
╔══════════════════════════════════════════════════════════════╗
║  COMPARISON RESULTS                                          ║
╚══════════════════════════════════════════════════════════════╝

Parsing Performance:
  Before:  84.18ms (median)
  After:   50.33ms (median)
  ⬆️ IMPROVED: +40.2% faster ✅

Symbols Parsed:
  Before:  1937
  After:   1937
  ✓ Unchanged
```

## ⚙️ Environment Variables

### ENABLE_BENCHMARK

Required for running benchmarks:

```bash
# Set before running script
export ENABLE_BENCHMARK=true

# Or inline
ENABLE_BENCHMARK=true ./run-benchmark.sh phase1
```

### Auto-Detection

The script automatically detects:
- Power mode (PluggedIn/Battery)
- Operating system
- .NET version
- Number of processors

## 🛠️ Troubleshooting

### "Not in a MQL4 Language Server project directory"

Make sure you're in the project root directory:

```bash
# Must contain these files:
ls Mql4LanguageServer.sln
ls run-benchmark.sh
```

### ".NET SDK not found"

Install .NET SDK 8.0 or later:

```bash
# Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install dotnet-sdk-10.0

# macOS
brew install --cask dotnet
```

### "Benchmark file not found"

The script moves files from `tests/bin/Debug/net10.0/benchmarks/` to the project root automatically. If this fails:

```bash
# Manually copy the file
cp tests/bin/Debug/net10.0/benchmarks/<milestone>.json benchmarks/
```

### "jq not installed" (for better comparison)

```bash
# Ubuntu/Debian
sudo apt-get install jq

# macOS
brew install jq

# Windows (with chocolatey)
choco install jq
```

## 📈 Best Practices

### 1. Always Compare Against Baseline

```bash
# Good: Compare against baseline
./run-benchmark.sh after-optimization-1

# Less useful: No comparison
./run-benchmark.sh after-optimization-1 --no-compare
```

### 2. Use Descriptive Names

```bash
# Good: Descriptive
./run-benchmark.sh after-completion-handler-optimization

# Less useful: Generic
./run-benchmark.sh test1
./run-benchmark.sh test2
```

### 3. Run on Plugged-In Power

```bash
# Check power mode before running
upower -i /org/freedesktop/UPower/devices/battery_BAT0 2>/dev/null | grep state:

# If on battery, plug in before benchmarking
```

### 4. Keep a History

```bash
# Create milestones at key points
./run-benchmark.sh before-optimization
# ... make changes ...
./run-benchmark.sh after-optimization
# ... make more changes ...
./run-benchmark.sh after-further-optimization
```

### 5. Use --force for Automation

```bash
# Good: Automated pipeline
ENABLE_BENCHMARK=true ./run-benchmark.sh automated-test --force

# This won't prompt for confirmation
```

## 🔧 Advanced Usage

### Script Integration

Use in CI/CD pipelines:

```bash
#!/bin/bash
# .github/workflows/benchmark.yml
- name: Run Performance Benchmark
  run: |
    export ENABLE_BENCHMARK=true
    ./run-benchmark.sh ci-commit-${{ github.sha }} --force --no-compare
```

### Bash Completion

Add to your `.bashrc`:

```bash
# Auto-complete milestone names
_benchmark_completion() {
    local cur=${COMP_WORDS[COMP_CWORD]}
    local milestones=$(ls benchmarks/*.json 2>/dev/null | xargs -I {} basename {} .json)
    COMPREPLY=( $(compgen -W "$milestones" -- "$cur") )
}
complete -F _benchmark_completion run-benchmark.sh
```

### Compare Multiple Files

```bash
# Compare two arbitrary milestones
diff <(jq . benchmarks/milestone1.json) <(jq . benchmarks/milestone2.json)

# Or use a more detailed comparison script
./compare-milestones.sh milestone1 milestone2
```

## 📝 Examples from Actual Use

### Before Cache Optimization

```bash
$ ./run-benchmark.sh before-cache
✅ Environment check passed
  Power Mode:   PluggedIn
  OS:           Debian GNU/Linux 13 (trixie) X64
  .NET Version: 10.0.0

▶ Running benchmark for milestone: before-cache

✅ Benchmark complete!
📁 Saved: benchmarks/before-cache.json

╔══════════════════════════════════════════════════════════════╗
║  Summary                                                    ║
╚══════════════════════════════════════════════════════════════╝
  File:  benchmarks/before-cache.json
  Date:  2025-11-24 19:00:00
```

### After Cache Optimization

```bash
$ ./run-benchmark.sh after-cache --compare

✅ Environment check passed

▶ Detecting system environment...

▶ Running benchmark for milestone: after-cache

✅ Benchmark complete!
📁 Saved: benchmarks/after-cache.json

╔══════════════════════════════════════════════════════════════╗
║  COMPARISON RESULTS                                          ║
╚══════════════════════════════════════════════════════════════╝

Parsing Performance:
  Before:  84.18ms (median)
  After:   45.62ms (median)
  ⬆️ IMPROVED: +45.8% faster ✅

Symbols Parsed:
  Before:  1937
  After:   1937
  ✓ Unchanged
```

## 🎉 Summary

The benchmark tool provides:

- ✅ Easy command-line interface
- ✅ Automatic environment detection
- ✅ Comparison with baseline
- ✅ Visual progress indicators
- ✅ Colored output
- ✅ Backup of overwritten files
- ✅ Detailed JSON output
- ✅ CI/CD friendly

For more information, run `./run-benchmark.sh --help`.
