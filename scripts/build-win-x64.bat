dotnet clean
dotnet restore
dotnet publish src\NexusMods.App\NexusMods.App.csproj -c Release -r win-x64 --output bin/win-x64 --self-contained -p:PublishReadyToRunComposite=true -p:PublishSingleFile=true