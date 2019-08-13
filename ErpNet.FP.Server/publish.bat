dotnet publish -r win-x86 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\win-x86
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\win-x64
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\osx-x64
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\linux-x64
dotnet publish -r linux-arm -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\linux-arm