#!/bin/sh

rm -rf bin/macos-arm64
dotnet clean
dotnet restore
dotnet publish src/NexusMods.App/NexusMods.App.csproj -c Release -r osx-arm64 --output bin/macos-arm64/NexusMods.app/Contents --self-contained -p:PublishReadyToRun=true #-p:PublishSingleFile=true
mkdir bin/macos/NexusMods.App/Contents/MacOS
cp -Rv build/macoOS/* bin/macos/NexusMods.app/Contents/MacOS
