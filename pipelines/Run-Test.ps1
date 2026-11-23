$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../tests/MagnumTweaker.Tests/MagnumTweaker.Tests.csproj'
$results = Join-Path $PSScriptRoot '../artifacts/test-results'
dotnet test $project --configuration Release --artifacts-path (Join-Path $PSScriptRoot '../artifacts/test') --settings (Join-Path $PSScriptRoot 'Test.runsettings') --results-directory $results --logger 'console;verbosity=detailed' --logger 'trx;LogFileName=MagnumTweaker.trx'
if ($LASTEXITCODE -ne 0) { throw "Tests failed with exit code $LASTEXITCODE." }
