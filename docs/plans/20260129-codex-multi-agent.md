# 計画: Codex向けマルチエージェント基盤（Windows/WSL + UI）

## Goal
WindowsのPowerShellから扱いやすく、Codex CLI を複数起動・統率できる基盤を `MonochromeMemory.CodexMultiAgent/` に新規作成する。役割は `config/roles.yaml` で定義し、UI で編集・運用できる。

## Design Check
- **Design Doc 必須**: はい（新機能・新アーキテクチャのため）

## 仕様前提（要承認）
- **実行基盤**: WSL + tmux を採用（多セッション制御を簡素化）
- **起動**: PowerShell から `wsl.exe` 経由で tmux/コマンドを起動
- **Codex CLI**: `codex` コマンドのみを前提
- **UI**: Svelte（ローカルサーバーで `localhost` に提供）
- **データ保存**: SQLite
- **Windows Terminal 連携**: 対象外（WSL + tmux で運用）

## Task List
- [x] プロジェクト構成案を設計（ディレクトリ構成、設定ファイル配置）
- [x] `docs/design/codex-multi-agent.md` を作成（アーキテクチャ、フロー、DBスキーマ）
- [ ] `MonochromeMemory.CodexMultiAgent/` のベース構成を作成（README, config, scripts の雛形）
- [ ] WSL + tmux 制御の基本スクリプト設計（PowerShell → wsl.exe）
- [ ] SQLite スキーマ設計（役割、タスク、実行履歴、ログ）
- [ ] Svelte UI の最低限画面設計（役割管理 / 起動停止 / タスク投入 / 進捗 / 履歴）
- [ ] 実装手順と運用フローのドキュメント化

## Affected Files
- `docs/plans/20260129-codex-multi-agent.md`
- `docs/design/codex-multi-agent.md`
- `MonochromeMemory.CodexMultiAgent/` 以下一式（新規）

## Risks
- WSL + tmux の導入が前提になるため、WSL 未導入環境では追加手順が必要
- Codex CLI の起動/停止の安定化（プロセス管理・ログ収集）
- ローカルサーバー/SQLite の同梱配布方法が未確定
