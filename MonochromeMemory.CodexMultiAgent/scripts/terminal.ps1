# Windows Terminal 連携（任意）
# 例: ログ監視用タブを開く

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$LogsPath = Join-Path $Root "runtime/logs"

if (-not (Get-Command wt.exe -ErrorAction SilentlyContinue)) {
  Write-Host "wt.exe が見つかりません。Windows Terminal が未インストールです。"
  exit 0
}

# 監視用の新規タブを開く（必要に応じて編集）
wt.exe -w 0 new-tab powershell -NoExit -Command "Get-ChildItem -Path '$LogsPath'"
