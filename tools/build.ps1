$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$bin = Join-Path $root 'bin'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $csc)) {
    throw "C# compiler was not found: $csc"
}

if (-not (Test-Path -LiteralPath $bin)) {
    New-Item -ItemType Directory -Path $bin | Out-Null
}

$coreSources = Get-ChildItem -LiteralPath (Join-Path $root 'src\ScheduleParser.Core') -Filter '*.cs' | ForEach-Object { $_.FullName }
$departmentSources = Get-ChildItem -LiteralPath (Join-Path $root 'src\ScheduleDepartmentApp') -Filter '*.cs' | ForEach-Object { $_.FullName }
$viewerSources = Get-ChildItem -LiteralPath (Join-Path $root 'src\ScheduleViewerApp') -Filter '*.cs' | ForEach-Object { $_.FullName }

$coreDll = Join-Path $bin 'ScheduleParser.Core.dll'
$departmentExe = Join-Path $bin 'ScheduleDepartmentApp.exe'
$viewerExe = Join-Path $bin 'ScheduleViewerApp.exe'
$webSource = Join-Path $root 'web'
$webTarget = Join-Path $bin 'web'

& $csc /nologo /codepage:65001 /target:library /out:$coreDll `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Xml.Linq.dll `
    /reference:System.IO.Compression.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    /reference:System.Web.Extensions.dll `
    $coreSources
if ($LASTEXITCODE -ne 0) { throw "Core build failed with exit code $LASTEXITCODE." }

& $csc /nologo /codepage:65001 /target:winexe /out:$departmentExe `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Xml.Linq.dll `
    /reference:System.IO.Compression.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    /reference:System.Web.Extensions.dll `
    /reference:$coreDll `
    $departmentSources
if ($LASTEXITCODE -ne 0) { throw "Department app build failed with exit code $LASTEXITCODE." }

& $csc /nologo /codepage:65001 /target:winexe /out:$viewerExe `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Web.Extensions.dll `
    /reference:$coreDll `
    $viewerSources
if ($LASTEXITCODE -ne 0) { throw "Viewer app build failed with exit code $LASTEXITCODE." }

if (Test-Path -LiteralPath $webSource) {
    $resolvedBin = [System.IO.Path]::GetFullPath($bin)
    $resolvedWebTarget = [System.IO.Path]::GetFullPath($webTarget)
    if (-not $resolvedWebTarget.StartsWith($resolvedBin + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean unexpected web target: $resolvedWebTarget"
    }
    if (Test-Path -LiteralPath $webTarget) {
        Remove-Item -LiteralPath $webTarget -Recurse -Force
    }
    Copy-Item -LiteralPath $webSource -Destination $webTarget -Recurse
}

Write-Host "Built:"
Write-Host "  $departmentExe"
Write-Host "  $viewerExe"
