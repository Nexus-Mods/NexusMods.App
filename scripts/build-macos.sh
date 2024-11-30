#!/bin/sh

# Clean up previous builds
rm -rf bin/macos-arm64 bin/macos-x64 bin/universal2

# Perform a clean build
dotnet clean
dotnet restore

# Build for ARM64
dotnet publish src/NexusMods.App/NexusMods.App.csproj -c Release -r osx-arm64 \
  --output bin/macos-arm64/NexusMods.app/Contents/MacOS \
  --self-contained -p:PublishReadyToRun=true

# Build for x86_64
dotnet publish src/NexusMods.App/NexusMods.App.csproj -c Release -r osx-x64 \
  --output bin/macos-x64/NexusMods.app/Contents/MacOS \
  --self-contained -p:PublishReadyToRun=true
# make the directory for the universal2 package
mkdir -p bin/universal2 
# Create universal2 directory structure
cp -Rv build/macOS/structure bin/universal2/NexusMods.app
cp -Rv bin/macos-arm64/NexusMods.app/Contents bin/universal2/NexusMods.app/
# Loop through the macos-arm64 directory and merge binaries with lipo
for file in bin/macos-arm64/NexusMods.app/Contents/MacOS/*; do
  if [[ $file == *.dylib || $file == *.lib || $file == *.dll || -x $file ]]; then
    base_name=$(basename "$file")
    arm_file="bin/macos-arm64/NexusMods.app/Contents/MacOS/$base_name"
    x64_file="bin/macos-x64/NexusMods.app/Contents/MacOS/$base_name"
    universal_file="bin/universal2/NexusMods.app/Contents/MacOS/$base_name"

    if [[ -f "$x64_file" ]]; then
      echo "Creating universal binary for $base_name"
      lipo -create -output "$universal_file" "$arm_file" "$x64_file"
    else
      echo "x86_64 version of $base_name not found, copying ARM64 version only."
      cp "$arm_file" "$universal_file"
    fi
  fi
done
 # Cleanup architecture-specific builds 
rm -rf bin/macos-arm64 bin/macos-x64

