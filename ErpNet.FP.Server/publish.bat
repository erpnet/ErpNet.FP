echo off

echo Publishing win-x86...
rmdir /Q /S Published\win-x86
dotnet publish -r win-x86 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\win-x86

IF /I "%1" EQU "test" EXIT /B

echo Publishing win-x64...
rmdir /Q /S Published\win-x64
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\win-x64

echo Publishing osx-x64...
rmdir /Q /S Published\osx-x64
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\osx-x64

echo Publishing linux-x64...
rmdir /Q /S Published\linux-x64
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\linux-x64

echo Publishing linux-arm...
rmdir /Q /S Published\linux-arm
dotnet publish -r linux-arm -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true -o Published\linux-arm