#!/bin/sh
dotnet publish -r win-x86 -c Release --self-contained -o ..\ErpNet.FP.Server\Published\win-x86
dotnet publish -r win-x64 -c Release --self-contained -o ..\ErpNet.FP.Server\Published\win-x64
