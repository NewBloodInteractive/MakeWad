@echo off
dotnet publish -c Release -r win-x64 -p:PublishAOT=true -p:DebuggerSupport=false -p:DebugType=none
