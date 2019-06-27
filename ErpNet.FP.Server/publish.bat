dotnet publish -r win-x86 -c Release --self-contained -o Published\win-x86
dotnet publish -r win-x64 -c Release --self-contained -o Published\win-x64
dotnet publish -r osx-x64 -c Release --self-contained -o Published\osx-x64
dotnet publish -r linux-x64 -c Release --self-contained -o Published\linux-x64
dotnet publish -r linux-arm -c Release --self-contained -o Published\linux-arm