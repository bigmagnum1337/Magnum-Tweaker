$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'src/MagnumTweaker/MagnumTweaker.csproj'
$distPath = Join-Path $PSScriptRoot 'dist'
$dist = New-Item -ItemType Directory -Path $distPath -Force
if ($dist.Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "The publish directory must be a regular folder: $distPath"
}

$stageName = '.publish-' + [Guid]::NewGuid().ToString('N')
$stage = New-Item -ItemType Directory -Path (Join-Path $dist.FullName $stageName)
$targetExe = Join-Path $dist.FullName 'Magnum Tweaker.exe'

try {
    $output = Join-Path $stage.FullName 'output'
    $artifacts = Join-Path $stage.FullName 'artifacts'
    dotnet publish $project -c Release -r win-x64 --self-contained true --artifacts-path $artifacts -o $output
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed (exit code $LASTEXITCODE). The previous EXE was kept."
    }

    $newExe = Get-Item -LiteralPath (Join-Path $output 'Magnum Tweaker.exe')
    if ($newExe.Length -le 0) { throw 'The published EXE is empty.' }

    if ([IO.File]::Exists($targetExe)) {
        $existing = Get-Item -LiteralPath $targetExe -Force
        if ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "The destination EXE must be a regular file: $targetExe"
        }
        try {
            # Windows may keep a running EXE mapped until the old process exits.
            $tempRoot = [IO.Path]::GetTempPath()
            $backupRoot = if ([IO.Path]::GetPathRoot($tempRoot) -eq [IO.Path]::GetPathRoot($targetExe)) { $tempRoot } else { $dist.Parent.FullName }
            $oldImage = Join-Path $backupRoot ("Magnum-previous-" + [Guid]::NewGuid().ToString("N") + ".tmp")
            [IO.File]::Replace($newExe.FullName, $targetExe, $oldImage)
            try { [IO.File]::Delete($oldImage) }
            catch [IO.IOException] { Write-Verbose "Previous image is still mapped: $oldImage" }
            catch [UnauthorizedAccessException] { Write-Verbose "Previous image is still mapped: $oldImage" }
        }
        catch [IO.IOException] {
            throw "Could not replace the previous EXE. Close Magnum Tweaker if it is running, then retry. $($_.Exception.Message)"
        }
    } else {
        [IO.File]::Move($newExe.FullName, $targetExe)
    }

    Write-Host "Portable build: $targetExe"
}
finally {
    $resolvedStage = Get-Item -LiteralPath $stage.FullName -Force -ErrorAction SilentlyContinue
    if ($resolvedStage -and $resolvedStage.Parent.FullName -eq $dist.FullName -and
        $resolvedStage.Name -eq $stageName -and -not ($resolvedStage.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        Remove-Item -LiteralPath $resolvedStage.FullName -Recurse -Force
    }
}
