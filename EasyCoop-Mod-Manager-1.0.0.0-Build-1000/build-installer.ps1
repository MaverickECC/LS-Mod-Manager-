$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishPath = Join-Path $projectRoot "publish"
$installerScript = Join-Path $projectRoot "installer\EasyCoop-Mod-Manager.iss"

& (Join-Path $projectRoot "verify-release.ps1")
if ($LASTEXITCODE -ne 0) { throw "Release-Pruefung ist fehlgeschlagen (Exitcode $LASTEXITCODE)." }

dotnet publish (Join-Path $projectRoot "EasyCoop.ModManager.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishPath
if ($LASTEXITCODE -ne 0) { throw "dotnet publish ist fehlgeschlagen (Exitcode $LASTEXITCODE). Installer wird nicht erstellt." }
if (-not (Test-Path (Join-Path $publishPath "EasyCoop Mod Manager.exe"))) {
    throw "Die veroeffentlichte Programmdatei fehlt. Installer wird nicht erstellt."
}

$innoCompiler = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $innoCompiler)) {
    throw "Inno Setup 6 wurde nicht gefunden. Bitte von https://jrsoftware.org/isinfo.php installieren."
}

& $innoCompiler $installerScript
if ($LASTEXITCODE -ne 0) { throw "Inno Setup ist fehlgeschlagen (Exitcode $LASTEXITCODE)." }
Write-Host "Installer erstellt: installer-output" -ForegroundColor Green
