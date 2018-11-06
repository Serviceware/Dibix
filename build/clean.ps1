$rootDirectory = Split-Path $MyInvocation.MyCommand.Path -Parent | Split-Path -Parent
Get-ChildItem $rootDirectory -Recurse -Include "bin", "obj", ".vs" | Remove-Item -Force -Recurse