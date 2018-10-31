@echo off
cd /d %~dp0\..
set /p connectionString=Enter connection string: 
set    providerName=System.Data.SqlClient
dotnet user-secrets set DefaultConnection:ConnectionString %connectionString% --id dibix
dotnet user-secrets set DefaultConnection:ProviderName %providerName% --id dibix

pause