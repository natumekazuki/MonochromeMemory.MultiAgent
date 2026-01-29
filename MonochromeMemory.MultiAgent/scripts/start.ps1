param(
  [switch]$OpenTerminal
)

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$Project = Join-Path $Root "app/CodexMultiAgent.App.csproj"

if (-not (Test-Path $Project)) {
  Write-Error "MAUI プロジェクトが見つかりません: $Project"
  exit 1
}

if ($OpenTerminal) {
  $TerminalScript = Join-Path $PSScriptRoot "terminal.ps1"
  if (Test-Path $TerminalScript) {
    & $TerminalScript
  }
}

# 開発時起動（EXE 配布時はビルド成果物を直接起動）
dotnet run --project $Project
