@echo off
set ASPNETCORE_ENVIRONMENT=Production
dotnet run --urls "http://localhost:5002" --environment Production
pause 