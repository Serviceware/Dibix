@echo off
for /d /r %~dp0\.. %%d in (bin obj .vs) do @if exist "%%d" echo "%%d" && rd /s/q "%%d"
pause