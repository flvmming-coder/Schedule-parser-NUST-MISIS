$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
$publish = Join-Path $root 'publish'
$staging = Join-Path $publish ('ScheduleParser-NUST-MISIS-v' + $version)
$zip = Join-Path $publish ('ScheduleParser-NUST-MISIS-v' + $version + '.zip')

& (Join-Path $PSScriptRoot 'build.ps1')

if (-not (Test-Path -LiteralPath $publish)) {
    New-Item -ItemType Directory -Path $publish | Out-Null
}

$resolvedPublish = [System.IO.Path]::GetFullPath($publish)
$resolvedStaging = [System.IO.Path]::GetFullPath($staging)
if (-not $resolvedStaging.StartsWith($resolvedPublish + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean unexpected staging path: $resolvedStaging"
}

if (Test-Path -LiteralPath $staging) {
    Remove-Item -LiteralPath $staging -Recurse -Force
}

New-Item -ItemType Directory -Path $staging | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'bin\ScheduleDepartmentApp.exe') -Destination $staging
Copy-Item -LiteralPath (Join-Path $root 'bin\ScheduleViewerApp.exe') -Destination $staging
Copy-Item -LiteralPath (Join-Path $root 'bin\ScheduleParser.Core.dll') -Destination $staging
Copy-Item -LiteralPath (Join-Path $root 'bin\web') -Destination $staging -Recurse
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination $staging
Copy-Item -LiteralPath (Join-Path $root 'CHANGELOG.md') -Destination $staging
Copy-Item -LiteralPath (Join-Path $root 'VERSION') -Destination $staging

if (Test-Path -LiteralPath $zip) {
    Remove-Item -LiteralPath $zip -Force
}

Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip
Write-Host "Package: $zip"
