$rootDirectory = Split-Path $MyInvocation.MyCommand.Path -Parent | Split-Path -Parent
Get-ChildItem $currentDirectory -Recurse -Include "bin", "obj", ".vs" | Remove-Item -Force -Recurse