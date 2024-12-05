#!/bin/bash

mkdir -p ./bin/macOS
cp -r "./src/NexusMods.App/dist/macOS/Nexus Mods.app" ./bin/macOS
dotnet publish "src/NexusMods.App/NexusMods.App.csproj" -c Release -r osx-arm64 -o "bin/macOS/Nexus Mods.app/Contents/MacOS"
