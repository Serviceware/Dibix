@echo off
for /d /r %~dp0\.. %%d in (.vs bin obj TestResults) do @if exist "%%d" echo "%%d" && rd /s/q "%%d"