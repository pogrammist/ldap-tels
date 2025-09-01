@echo off
set ASPNETCORE_ENVIRONMENT=Staging
dotnet run --urls "http://localhost:5001" --environment Staging
pause 