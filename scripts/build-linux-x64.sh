dotnet clean
dotnet restore
dotnet publish src/NexusMods.App/NexusMods.App.csproj -c Release -r linux-x64 --output bin/linux-x64 --self-contained -p:PublishReadyToRunComposite=true -p:PublishSingleFile=true
