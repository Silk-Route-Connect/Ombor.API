<#
.SYNOPSIS
Cleans old TestResults/coverage folders, runs the tests with Coverlet,
builds an HTML report, and (optionally) opens it in the default browser.
#>

param(
  [string]$SolutionPath = ".", # path to *.sln or folder
  [switch]$NoOpen # use -NoOpen to skip launching the browser
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host "› $msg" -ForegroundColor Cyan }

# 1. Clean previous artifacts
Write-Info "Cleaning TestResults and coverage folders…"
Get-ChildItem -Path $SolutionPath -Include TestResults, coverage -Recurse |
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# 2. Run tests + collect coverage
Write-Info "Running dotnet test…"
dotnet test $SolutionPath `
  --collect:"XPlat Code Coverage" `
  --settings coverlet.runsettings `
  --nologo

# 3. Generate HTML report
Write-Info "Generating HTML report…"
reportgenerator `
  -reports:"$SolutionPath\Tests\**\TestResults\**\coverage.cobertura.xml" `
  -targetdir:"$SolutionPath\coverage" `
  -reporttypes:Html

# 4. Open report (optional)
if (-not $NoOpen) {
  Start-Process "$SolutionPath\coverage\index.html"
}
