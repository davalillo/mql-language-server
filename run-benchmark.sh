#!/bin/bash
set -euo pipefail

# ================================================================
# MQL4 Language Server - Benchmark Runner
# ================================================================
# Usage: ./run-benchmark.sh <milestone> [options]
#
# Options:
#   --compare      Show comparison vs baseline-initial.json (default)
#   --no-compare   Skip comparison
#   --force        Overwrite existing milestone without asking
#   --help         Show this help message
#
# Examples:
#   ./run-benchmark.sh phase1
#   ./run-benchmark.sh after-completion-handler --no-compare
#   ./run-benchmark.sh phase1 --force
#
# Environment Variables:
#   ENABLE_BENCHMARK=true (required)
# ================================================================

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
PURPLE='\033[0;35m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# Configuration
DOTNET_PROJECT="tests/Mql4LanguageServer.Tests.csproj"
FILTER="BaselineBenchmark"
BASELINE_FILE="benchmarks/baseline-initial.json"
MAX_BACKUPS=5

# ================================================================
# Helper Functions
# ================================================================

print_header() {
    echo -e "${CYAN}${BOLD}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}${BOLD}║${NC}  MQL4 Language Server - Benchmark Runner${NC}"
    echo -e "${CYAN}${BOLD}╚══════════════════════════════════════════════════════════════╝${NC}"
    echo ""
}

print_error() {
    echo -e "${RED}${BOLD}❌ ERROR:${NC} $1" >&2
}

print_success() {
    echo -e "${GREEN}${BOLD}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

print_step() {
    echo -e "${PURPLE}${BOLD}▶ $1${NC}"
}

usage() {
    cat << EOF
Usage: $(basename "$0") <milestone> [options]

Options:
    --compare      Show comparison vs baseline-initial.json (default)
    --no-compare   Skip comparison
    --force        Overwrite existing milestone without asking
    --help         Show this help message

Examples:
    $(basename "$0") phase1
    $(basename "$0") after-completion-handler --no-compare
    $(basename "$0") phase1 --force

Environment Variables:
    ENABLE_BENCHMARK=true (required for benchmarks)

EOF
    exit 0
}

check_environment() {
    print_step "Checking environment..."

    # Check if we're in a .NET project
    if [ ! -f "Mql4LanguageServer.sln" ]; then
        print_error "Not in a MQL4 Language Server project directory"
        print_info "Run this script from the project root directory"
        exit 1
    fi

    # Check if .NET SDK is available
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found"
        print_info "Install .NET SDK 8.0 or later"
        exit 1
    fi

    # Check if test project exists
    if [ ! -f "$DOTNET_PROJECT" ]; then
        print_error "Test project not found: $DOTNET_PROJECT"
        exit 1
    fi

    # Check ENABLE_BENCHMARK
    if [ "${ENABLE_BENCHMARK:-}" != "true" ]; then
        print_warning "ENABLE_BENCHMARK is not set to 'true'"
        print_info "Benchmarks are disabled. Set ENABLE_BENCHMARK=true to run benchmarks."
        print_info "Continuing anyway (test will be skipped)..."
    fi

    print_success "Environment check passed"
    echo ""
}

detect_environment() {
    print_step "Detecting system environment..."

    # Power mode detection
    if command -v upower &> /dev/null && upower -i /org/freedesktop/UPower/devices/battery_BAT0 2>/dev/null | grep -q "state:"; then
        POWER_MODE=$(upower -i /org/freedesktop/UPower/devices/battery_BAT0 2>/dev/null | grep "state:" | awk '{print $2}')
        if [[ "$POWER_MODE" == "charging" || "$POWER_MODE" == "fully-charged" ]]; then
            POWER_MODE_DISPLAY="${GREEN}PluggedIn${NC}"
        elif [[ "$POWER_MODE" == "discharging" ]]; then
            POWER_MODE_DISPLAY="${YELLOW}Battery${NC}"
        else
            POWER_MODE_DISPLAY="${BLUE}Unknown${NC}"
        fi
    elif command -v pmset &> /dev/null; then
        # macOS
        if pmset -g batt | grep -q "AC Power"; then
            POWER_MODE_DISPLAY="${GREEN}PluggedIn${NC}"
        else
            POWER_MODE_DISPLAY="${YELLOW}Battery${NC}"
        fi
    else
        POWER_MODE_DISPLAY="${BLUE}Unknown${NC}"
    fi

    # OS detection
    if command -v uname &> /dev/null; then
        OS_INFO=$(uname -srmo)
        OS_DISPLAY="${BLUE}$OS_INFO${NC}"
    else
        OS_DISPLAY="${BLUE}Unknown${NC}"
    fi

    # .NET version
    DOTNET_VERSION=$(dotnet --version 2>/dev/null || echo "Unknown")
    DOTNET_DISPLAY="${BLUE}$DOTNET_VERSION${NC}"

    echo -e "  Power Mode:   $POWER_MODE_DISPLAY"
    echo -e "  OS:           $OS_DISPLAY"
    echo -e "  .NET Version: $DOTNET_DISPLAY"
    echo ""

    # Warning if on battery
    if [[ "$POWER_MODE_DISPLAY" == *"${YELLOW}Battery${NC}"* ]]; then
        print_warning "Running on battery power"
        print_info "Benchmarks may be slower and less accurate"
        echo ""
    fi
}

check_milestone_exists() {
    local milestone=$1
    local force=$2
    local file="benchmarks/${milestone}.json"

    if [ -f "$file" ]; then
        if [ "$force" == "true" ]; then
            print_warning "Milestone '$milestone' already exists (overwriting)"
            return 0
        fi

        echo -n -e "${YELLOW}Milestone '$milestone' already exists. Overwrite? [y/N]${NC} "
        read -r response
        if [[ ! "$response" =~ ^[Yy]$ ]]; then
            print_info "Cancelled"
            exit 0
        fi

        # Backup existing file
        backup_existing_file "$file"
    fi
}

backup_existing_file() {
    local file=$1
    local dir=$(dirname "$file")
    local base=$(basename "$file" .json)

    # Create backups directory if it doesn't exist
    mkdir -p "$dir/backups"

    # Find next backup number
    local i=1
    while [ -f "$dir/backups/${base}-backup${i}.json" ] && [ $i -lt $MAX_BACKUPS ]; do
        i=$((i + 1))
    done

    # If we've reached max backups, shift them
    if [ $i -ge $MAX_BACKUPS ]; then
        # Remove oldest
        rm -f "$dir/backups/${base}-backup1.json"
        # Shift others
        for j in $(seq 2 $MAX_BACKUPS); do
            local prev=$((j - 1))
            if [ -f "$dir/backups/${base}-backup${j}.json" ]; then
                mv "$dir/backups/${base}-backup${j}.json" "$dir/backups/${base}-backup${prev}.json"
            fi
        done
        i=$((MAX_BACKUPS))
    fi

    # Create new backup
    cp "$file" "$dir/backups/${base}-backup${i}.json"
    print_info "Backed up existing file to $dir/backups/${base}-backup${i}.json"
}

run_benchmark() {
    local milestone=$1

    print_step "Running benchmark for milestone: ${BOLD}$milestone${NC}"
    echo ""

    # Create benchmarks directory if it doesn't exist
    mkdir -p benchmarks

    # Run the benchmark
    ENABLE_BENCHMARK=true BENCHMARK_MILESTONE="$milestone" \
        dotnet test "$DOTNET_PROJECT" \
        --filter "$FILTER" \
        --logger "console;verbosity=detailed" \
        --no-build \
        2>&1 | tee /tmp/benchmark-output.tmp

    # Check if benchmark completed successfully
    if grep -q "Benchmark complete" /tmp/benchmark-output.tmp; then
        print_success "Benchmark completed successfully"
        echo ""

        # Move file from bin directory if needed
        if [ ! -f "benchmarks/${milestone}.json" ] && [ -f "tests/bin/Debug/net10.0/benchmarks/${milestone}.json" ]; then
            print_info "Moving benchmark file to project root..."
            cp "tests/bin/Debug/net10.0/benchmarks/${milestone}.json" "benchmarks/"
            print_success "File moved to benchmarks/${milestone}.json"
        fi

        return 0
    else
        print_error "Benchmark failed or did not complete"
        return 1
    fi
}

compare_results() {
    local current_file=$1
    local baseline_file=$2

    if [ ! -f "$baseline_file" ]; then
        print_warning "Baseline file not found: $baseline_file"
        print_info "Skipping comparison (run baseline first)"
        return 0
    fi

    if [ ! -f "$current_file" ]; then
        print_error "Current benchmark file not found: $current_file"
        return 1
    fi

    echo ""
    print_step "Comparing results..."
    echo ""

    # Use jq to extract values if available
    if command -v jq &> /dev/null; then
        compare_with_jq "$baseline_file" "$current_file"
    else
        compare_without_jq "$baseline_file" "$current_file"
    fi
}

compare_with_jq() {
    local baseline_file=$1
    local current_file=$2

    local baseline_parsing=$(jq -r '.Parsing.Median' "$baseline_file")
    local current_parsing=$(jq -r '.Parsing.Median' "$current_file")
    local baseline_symbols=$(jq -r '.SymbolCount.Median' "$baseline_file")
    local current_symbols=$(jq -r '.SymbolCount.Median' "$current_file")

    local improvement=$(echo "scale=2; ($baseline_parsing - $current_parsing) / $baseline_parsing * 100" | bc)

    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${CYAN}${BOLD}COMPARISON RESULTS${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""

    # Parsing Performance
    echo -e "${BOLD}Parsing Performance:${NC}"
    printf "  Before:  %.2fms (median)\n" "$baseline_parsing"
    printf "  After:   %.2fms (median)\n" "$current_parsing"

    if (( $(echo "$improvement > 0" | bc -l) )); then
        printf "  %b IMPROVED: +%.1f%% faster %b\n" "${GREEN}" "$improvement" "✅" "${NC}"
    elif (( $(echo "$improvement < 0" | bc -l) )); then
        local degradation=$(echo "$improvement" | tr -d '-')
        printf "  %b REGRESSED: -%.1f%% slower %b\n" "${RED}" "$degradation" "❌" "${NC}"
    else
        printf "  %b UNCHANGED %b\n" "${YELLOW}" "${NC}"
    fi
    echo ""

    # Symbols
    echo -e "${BOLD}Symbols Parsed:${NC}"
    printf "  Before:  %.0f\n" "$baseline_symbols"
    printf "  After:   %.0f\n" "$current_symbols"

    if [ "$baseline_symbols" == "$current_symbols" ]; then
        printf "  %b Unchanged %b\n" "${GREEN}✓${NC} ${NC}"
    else
        printf "  %b Changed %b\n" "${YELLOW}~${NC} ${NC}"
    fi

    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

compare_without_jq() {
    local baseline_file=$1
    local current_file=$2

    print_info "jq not installed, skipping detailed comparison"
    print_info "Install jq for better comparison: sudo apt-get install jq (Linux) or brew install jq (macOS)"
}

print_summary() {
    local milestone=$1
    local file="benchmarks/${milestone}.json"

    if [ ! -f "$file" ]; then
        return 0
    fi

    echo ""
    echo -e "${GREEN}${BOLD}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}${BOLD}║${NC}  Summary${NC}"
    echo -e "${GREEN}${BOLD}╚══════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    echo -e "  File:  ${BOLD}$file${NC}"
    echo -e "  Date:  $(date '+%Y-%m-%d %H:%M:%S')"
    echo ""
}

# ================================================================
# Main Script
# ================================================================

main() {
    # Parse arguments
    local milestone=""
    local force=false
    local do_compare=true

    if [ $# -lt 1 ] || [ "$1" == "--help" ] || [ "$1" == "-h" ]; then
        usage
    fi

    milestone=$1
    shift || true

    # Parse options
    while [ $# -gt 0 ]; do
        case "$1" in
            --force)
                force=true
                shift
                ;;
            --no-compare)
                do_compare=false
                shift
                ;;
            --compare)
                do_compare=true
                shift
                ;;
            *)
                print_error "Unknown option: $1"
                usage
                ;;
        esac
    done

    # Validate milestone name
    if [[ ! "$milestone" =~ ^[a-zA-Z0-9_-]+$ ]]; then
        print_error "Invalid milestone name: $milestone"
        print_info "Milestone names can only contain letters, numbers, hyphens, and underscores"
        exit 1
    fi

    # Print header
    print_header

    # Check environment
    check_environment

    # Detect environment
    detect_environment

    # Check if milestone exists
    check_milestone_exists "$milestone" "$force"

    # Run benchmark
    if ! run_benchmark "$milestone"; then
        print_error "Benchmark failed"
        exit 1
    fi

    # Compare results
    if [ "$do_compare" == true ]; then
        compare_results "benchmarks/${milestone}.json" "$BASELINE_FILE"
    fi

    # Print summary
    print_summary "$milestone"

    print_success "All done! 🎉"
}

# Run main function
main "$@"
