$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot '../Publish.ps1')

$dist = Join-Path $PSScriptRoot '../dist'
$portable = Join-Path $dist 'Magnum Tweaker.exe'
$files = @(Get-ChildItem -LiteralPath $dist -Force)
if ($files.Count -ne 1 -or !(Test-Path -LiteralPath $portable -PathType Leaf)) {
    throw 'Expected exactly one portable EXE in dist.'
}
