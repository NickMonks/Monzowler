#!/bin/bash

set -e

PROJECT_PATH="./src/api/Monzowler.CLI"
OUTPUT_DIR="./releases"
CONFIGURATION="Release"

RUNTIMES=(
  "win-x64"
  "linux-x64"
  "linux-arm64"
  "osx-x64"
  "osx-arm64"
)

echo "Building self-contained binaries..."
for RID in "${RUNTIMES[@]}"; do
  OUT_PATH="$OUTPUT_DIR/$RID"
  echo "🔧 Building for $RID..."

  dotnet publish "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -r "$RID" \
    --self-contained true \
    --output "$OUT_PATH" \
    /p:PublishSingleFile=false \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    /p:PublishTrimmed=false

  echo "✅ Output: $OUT_PATH"
done

echo "🎉 All binaries built successfully."