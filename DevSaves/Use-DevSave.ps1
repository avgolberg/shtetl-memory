# Installs a development test save as the game's saveData.json, backing up the current save first.
# Usage (Unity must not be in Play mode):
#   powershell -ExecutionPolicy Bypass -File DevSaves\Use-DevSave.ps1 l2_street_end_post_intro
# Restore a backup by copying a saveData.backup-*.json over saveData.json in the same folder.
# The save folder follows Player Settings company/product names (currently DefaultCompany / "Messing Around").
param([Parameter(Mandatory = $true)][string]$Preset)

$presetPath = Join-Path $PSScriptRoot "$Preset.json"
if (-not (Test-Path $presetPath)) { throw "Preset not found: $presetPath" }

$saveDir = Join-Path $env:USERPROFILE 'AppData\LocalLow\DefaultCompany\Messing Around'
$savePath = Join-Path $saveDir 'saveData.json'
New-Item -ItemType Directory -Force $saveDir | Out-Null

if (Test-Path $savePath) {
    $backup = Join-Path $saveDir ("saveData.backup-{0:yyyyMMdd-HHmmss}.json" -f (Get-Date))
    Copy-Item $savePath $backup
    "Backed up current save to $backup"
}

Copy-Item $presetPath $savePath -Force
"Installed preset '$Preset' as $savePath"
