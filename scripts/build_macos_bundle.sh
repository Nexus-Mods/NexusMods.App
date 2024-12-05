#!/bin/bash

mkdir -p ./bin/macOS
cp -r ./src/NexusMods.App/dist/macOS/NexusMods.app ./bin/macOS
dotnet publish src/NexusMods.App/NexusMods.App.csproj -c Release -r osx-arm64 -o bin/macOS/NexusMods.app/Contents/MacOS
