# 計画: Codex向けマルチエージェント基盤（Windowsネイティブ + UI）

## Goal
WindowsのPowerShellから扱いやすく、Codex CLI を複数起動・統率できる基盤を `MonochromeMemory.CodexMultiAgent/` に新規作成する。役割は `config/roles.yaml` で定義し、UI で編集・運用できる。

## Design Check
- **Design Doc 必須**: はい（新機能・新アーキテクチャのため）

## 仕様前提（要承認）
- **実行基盤**: Windows ネイティブ（PowerShell 前提）
- **Codex CLI**: `codex` コマンドのみを前提
- **TTY必須**: PTY制御で複数セッションを起動
- **UI**: Svelte（ローカルサーバーで `localhost` に提供）
- **データ保存**: SQLite
- **Windows Terminal 連携**: 可能なら組み込み（補助的機能）

## Task List
- [x] プロジェクト構成案を設計（ディレクトリ構成、設定ファイル配置）
- [x] `docs/design/codex-multi-agent.md` を作成（アーキテクチャ、フロー、DBスキーマ）
- [ ] `MonochromeMemory.CodexMultiAgent/` のベース構成を作成（README, config, scripts の雛形）
- [ ] PTY制御（node-pty/ConPTY）を使ったセッション管理設計
- [ ] Windows Terminal 連携（可能なら）設計
- [ ] SQLite スキーマ設計（役割、タスク、実行履歴、ログ）
- [ ] Svelte UI の最低限画面設計（役割管理 / 起動停止 / タスク投入 / 進捗 / 履歴）
- [ ] 実装手順と運用フローのドキュメント化

## Affected Files
- `docs/plans/20260129-codex-multi-agent.md`
- `docs/design/codex-multi-agent.md`
- `MonochromeMemory.CodexMultiAgent/` 以下一式（新規）

## Risks
- node-pty/ConPTY の互換性（Windows環境差異）
- Codex CLI の起動/停止の安定化（PTY配下のプロセス管理）
- Windows Terminal 連携の実装コスト（補助機能の扱い）
- ローカルサーバー/SQLite の同梱配布方法が未確定

## Notes / Logs
- 2026-01-29: WSL前提を撤回し、Windowsネイティブ（PowerShell）前提に変更。TTY必須のため PTY制御を採用し、Windows Terminal 連携は補助機能として検討する。
