param(
    # Root of the per-machine rig configuration directories. The rig workflow reads
    # <ConfigRoot>\<MACHINE-NAME>\rig.yml and calibration artefacts beside it. The template
    # default is the repository's own config\rigs folder (ignored by git), which matches the
    # ConfigRoot property of src\main.bonsai. Once rigs share a root, such as C:\RigConfigs or a
    # network share, pass it here and set the same value on the workflow.
    [string]$ConfigRoot = ".\config\rigs"
)

$ErrorActionPreference = "Stop"
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDirectory = Split-Path -Parent $scriptPath
Set-Location (Split-Path -Parent $scriptDirectory)

# Set-Location moves PowerShell but not the .NET file APIs used below. Make the root absolute here.
if (-not [IO.Path]::IsPathRooted($ConfigRoot)) {
    $ConfigRoot = Join-Path (Get-Location).Path $ConfigRoot
}
$ConfigRoot = [IO.Path]::GetFullPath($ConfigRoot)

Write-Output "Initializing and updating submodules..."
&git submodule update --init --recursive

if (-not (Get-Command uv -ErrorAction SilentlyContinue)) {
    throw "The 'uv' command was not found. See https://docs.astral.sh/uv/getting-started/installation/ for instructions."
}

Write-Output "Creating a Python environment..."
if (Test-Path -Path ./.venv) {
    Remove-Item ./.venv -Recurse -Force
}
&uv venv
.\.venv\Scripts\Activate.ps1
Write-Output "Synchronizing environment..."
&uv sync

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The 'dotnet' command was not found. Install the .NET SDK: https://dotnet.microsoft.com/download"
}

Write-Output "Restoring dotnet tools..."
&dotnet tool restore

# One command for everything derived from the models: the JSON schema, the C# classes and the
# rig template. The namespace of the C# classes follows the schema file name.
Write-Output "Regenerating the schema, the C# classes and the rig template..."
&uv run regenerate
if ($LASTEXITCODE -ne 0) { throw "regenerate failed; see the output above." }

# Same name the rig workflow resolves at startup (Environment.MachineName).
$machineDirectory = Join-Path $ConfigRoot ([Environment]::MachineName)
Write-Output "Preparing the rig configuration directory $machineDirectory..."
New-Item -ItemType Directory -Path (Join-Path $machineDirectory "calibration") -Force | Out-Null
$rigConfig = Join-Path $machineDirectory "rig.yml"
if (Test-Path -Path $rigConfig) {
    Write-Output "Keeping the existing $rigConfig"
} else {
    # The template's $schema path is relative to config\; point the copy at this checkout instead.
    $schemaDirectory = (Resolve-Path ".\src\DataSchemas").Path -replace "\\", "/"
    $text = [IO.File]::ReadAllText((Resolve-Path ".\config\rig-template.yml").Path)
    $text = $text.Replace('$schema=../src/DataSchemas/', '$schema=' + $schemaDirectory + '/')
    [IO.File]::WriteAllText($rigConfig, $text)
    Write-Output "Created $rigConfig from config\rig-template.yml; fill in this rig's values there."
}
