# 再開メモ: CLIマルチエージェント基盤（Windowsネイティブ / MAUI）

## 現状サマリ
- Windows 11 前提、MAUI Blazor でネイティブアプリ化する方針。
- WSL/tmux は不採用。TTY 必須の CLI を ConPTY で起動する設計。
- 役割名は一般化済み: オーケストレーター / ディスパッチャー / エージェント。
- スキル生成は参考実装踏襲。アプリ側で自動生成せず、**エージェント報告 → 承認 → ディスパッチャー作成**の流れ。
- スキルはアプリ配下の正本に保存し、**コピー**で `~/.codex/skills` と `~/.copilot/skills` へ連携（シンボリックリンクなし）。

## 重要な設計決定
- UI: MAUI Blazor（Svelte 案は撤回）
- CLI 起動: 役割ごとに任意コマンド（Codex専用ではない）
- ConPTY 実装: Win32 API を P/Invoke で直接利用（ベスプラ方針は設計書に追記済み）
- Windows Terminal 連携: 任意の補助機能（主経路は MAUI UI）

## 変更済みファイル・構成
- MAUI 基盤: `MonochromeMemory.MultiAgent/app/`
- 役割設定: `MonochromeMemory.MultiAgent/config/roles.yaml`
- 設定: `MonochromeMemory.MultiAgent/config/settings.yaml`
- 役割ファイル:
  - `MonochromeMemory.MultiAgent/roles/orchestrator.md`
  - `MonochromeMemory.MultiAgent/roles/dispatcher.md`
  - `MonochromeMemory.MultiAgent/roles/agent1.md`
- ランタイム YAML:
  - `MonochromeMemory.MultiAgent/runtime/queue/tasks/*.yaml`
  - `MonochromeMemory.MultiAgent/runtime/queue/reports/*.yaml`
  - `MonochromeMemory.MultiAgent/runtime/queue/commands/dispatcher.yaml`
- スキル正本: `MonochromeMemory.MultiAgent/skills/registry/`
- ConPTY ラッパー:
  - `MonochromeMemory.MultiAgent/app/Services/Pty/ConPtyNative.cs`
  - `MonochromeMemory.MultiAgent/app/Services/Pty/ConPtySession.cs`
  - `MonochromeMemory.MultiAgent/app/Services/Pty/CommandLineBuilder.cs`
  - `MonochromeMemory.MultiAgent/app/Services/Pty/PtyStartOptions.cs`

## 起動方法（開発）
```powershell
./scripts/start.ps1
```

## 未着手/保留のタスク
- Orchestrator Service 実装（roles.yaml 読み込み→ConPTY起動→出力をUIへ）
- MAUI UI 画面設計（役割管理 / 起動停止 / タスク投入 / 進捗 / 履歴）
- SQLite 実装（スキーマ反映と保存/読取）
- スキル連携のコピー処理実装（正本 → Codex/Copilot 個人用）
- `runtime/queue/commands/dispatcher.yaml` のスキル作成指示フォーマット確定
- Windows Terminal 連携（任意）

## 次回最初に確認すべきこと
- `docs/design/codex-multi-agent.md` の内容と最新整合
- 役割 ID/ファイル名の整合（orchestrator/dispatcher/agent1）
- ConPTY ラッパーの起動テスト（簡易起動サンプル）

