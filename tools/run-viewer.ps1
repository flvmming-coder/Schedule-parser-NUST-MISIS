$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'bin\ScheduleViewerApp.exe'

if (-not (Test-Path -LiteralPath $exe)) {
    & (Join-Path $PSScriptRoot 'build.ps1')
}

Start-Process -FilePath $exe
