#!/bin/bash

# Script para ejecutar los tests de Memory Profiling del MQL Language Server
#
# USO:
#   ./run-memory-test.sh                    # Ejecutar con milestone por defecto: "memory-test"
#   ./run-memory-test.sh mi-benchmark       # Ejecutar con milestone personalizado
#
# EJEMPLO:
#   ./run-memory-test.sh fase5-final
#
# El script ejecutará el test de Memory Profiling y mostrará un resumen del benchmark.

set -e

# Colores para output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Banner
echo -e "${BLUE}============================================${NC}"
echo -e "${BLUE}  MQL4 LSP - Memory Profiling Test${NC}"
echo -e "${BLUE}============================================${NC}"
echo ""

# Verificar que dotnet esté disponible
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}ERROR: dotnet no está instalado o no está en el PATH${NC}"
    exit 1
fi

# Determinar milestone name
MILESTONE=${1:-"memory-test"}
echo -e "${YELLOW}Milestone name:${NC} $MILESTONE"
echo ""

# Ejecutar el test
echo -e "${BLUE}Ejecutando test de Memory Profiling...${NC}"
echo -e "${YELLOW}Comando:${NC} ENABLE_BENCHMARK=true BENCHMARK_MILESTONE=$MILESTONE dotnet test tests/ --filter 'MemoryProfilingTests'"
echo ""

# Ejecutar con environment variables
export ENABLE_BENCHMARK=true
export BENCHMARK_MILESTONE=$MILESTONE

# Ejecutar el test desde el directorio raíz del proyecto
cd "$(dirname "$0")"
PROJECT_ROOT=$(pwd)

# Ejecutar el test y capturar exit code
if ENABLE_BENCHMARK=true BENCHMARK_MILESTONE=$MILESTONE dotnet test tests/ --filter 'MemoryProfilingTests' --verbosity normal; then
    echo ""
    echo -e "${GREEN}✅ Test de Memory Profiling completado exitosamente${NC}"
    echo ""

    # Verificar si se guardó el archivo de benchmark
    # El archivo puede estar en varias ubicaciones dependiendo de cómo se ejecute el test
    BENCHMARK_FILE_CANDIDATES=(
        "benchmarks/${MILESTONE}-memory.json"
        "tests/bin/Debug/net10.0/benchmarks/${MILESTONE}-memory.json"
        "tests/bin/Release/net10.0/benchmarks/${MILESTONE}-memory.json"
    )

    BENCHMARK_FILE_FOUND=""
    for candidate in "${BENCHMARK_FILE_CANDIDATES[@]}"; do
        if [ -f "$candidate" ]; then
            BENCHMARK_FILE_FOUND="$candidate"
            break
        fi
    done

    if [ -n "$BENCHMARK_FILE_FOUND" ]; then
        echo -e "${GREEN}📊 Benchmark guardado:${NC} $BENCHMARK_FILE_FOUND"
        echo ""
        echo -e "${BLUE}Resumen del benchmark:${NC}"
        grep -E '"MemoryGrowthPercent"|"HasMemoryLeak"|"StressTestFiles"|"InitialMemoryMB"|"FinalMemoryMB"' "$BENCHMARK_FILE_FOUND"
    else
        echo -e "${YELLOW}⚠️  Archivo de benchmark no encontrado${NC}"
        echo -e "${YELLOW}   Esto puede suceder si el test fue saltado (necesita ENABLE_BENCHMARK=true)${NC}"
        echo -e "${YELLOW}   Directorio actual: $PROJECT_ROOT${NC}"
        echo -e "${YELLOW}   Buscado en:${NC}"
        for candidate in "${BENCHMARK_FILE_CANDIDATES[@]}"; do
            echo -e "${YELLOW}     - $candidate${NC}"
        done
    fi

    exit 0
else
    echo ""
    echo -e "${RED}❌ Test de Memory Profiling falló${NC}"
    echo ""
    exit 1
fi
