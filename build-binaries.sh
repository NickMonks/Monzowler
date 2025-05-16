#!/bin/bash

set -e

PROJECT_PATH="./src/api/Monzowler.CLI"
OUTPUT_DIR="./releases"
CONFIGURATION="Release"
VERSION=$(git describe --tags --abbrev=0 || echo "v0.0.0")

RUNTIMES=(
  "win-x64"
  "linux-x64"
  "linux-arm64"
  "osx-x64"
  "osx-arm64"
)

echo "Building Monzowler CLI $VERSION binaries..."

for RID in "${RUNTIMES[@]}"; do
  OUT_PATH="$OUTPUT_DIR/$VERSION/$RID"
  echo "Building for $RID..."

  dotnet publish "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -r "$RID" \
    --self-contained true \
    --output "$OUT_PATH" \
    /p:PublishSingleFile=false \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    /p:PublishTrimmed=false

  echo "Build completed: $OUT_PATH"

  cd "$OUT_PATH"

  if [[ "$RID" == win-* ]]; then
    ZIP_FILE="../../monzowler-cli-$RID.zip"
    zip -r "$ZIP_FILE" ./*
  else
    TAR_FILE="../../monzowler-cli-$RID.tar.gz"
    tar -czf "$TAR_FILE" ./*
  fi

  cd - > /dev/null
done

echo "All binaries built and compressed successfully."