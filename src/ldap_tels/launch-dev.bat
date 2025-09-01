@echo off
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --urls "http://localhost:5000" --environment Development
pause 