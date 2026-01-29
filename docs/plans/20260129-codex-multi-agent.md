# 計画: CLIマルチエージェント基盤（Windowsネイティブ / MAUI）

## Goal
WindowsのPowerShellから扱いやすく、任意のCLIコーディングエージェントを複数起動・統率できる基盤を `MonochromeMemory.CodexMultiAgent/` に新規作成する。役割は `config/roles.yaml` で定義し、MAUI Blazor UI で編集・運用できる。

## Design Check
- **Design Doc 必須**: はい（新機能・新アーキテクチャのため）

## 仕様前提（要承認）
- **実行基盤**: Windows ネイティブ（PowerShell 前提）
- **CLI実行**: 役割ごとに任意コマンドを設定可能
- **TTY必須**: PTY制御で複数セッションを起動
- **UI**: MAUI Blazor（Windowsネイティブアプリ）
- **データ保存**: SQLite
- **Windows Terminal 連携**: 任意（補助的機能）
- **OS**: Windows 11 前提
- **配布**: まずはビルドして EXE 生成まで

## Task List
- [x] プロジェクト構成案を設計（ディレクトリ構成、設定ファイル配置）
- [x] `docs/design/codex-multi-agent.md` を作成（アーキテクチャ、フロー、DBスキーマ）
- [x] `MonochromeMemory.CodexMultiAgent/` のベース構成を作成（README, config, scripts の雛形）
- [x] MAUI Blazor アプリ基盤の作成（UI + API + データアクセス）
- [x] ConPTY を使ったセッション管理設計（ライブラリ方針決定）
- [x] ConPTY ラッパー実装（P/Invoke ベースの基礎）
- [x] MAUI アプリを Windows 11 専用ターゲットへ調整
- [ ] Windows Terminal 連携（任意）の設計
- [ ] スキル候補の自動生成フロー設計（Codex/Copilot）
- [ ] スキル出力（Codex/Copilot）フォーマットと保存先の実装
- [x] SQLite スキーマ設計（役割、タスク、実行履歴、ログ）
- [ ] MAUI UI の最低限画面設計（役割管理 / 起動停止 / タスク投入 / 進捗 / 履歴）
- [ ] 実装手順と運用フローのドキュメント化

## Affected Files
- `docs/plans/20260129-codex-multi-agent.md`
- `docs/design/codex-multi-agent.md`
- `MonochromeMemory.CodexMultiAgent/` 以下一式（新規）

## Risks
- ConPTY の互換性（Windows環境差異）
- CLI の起動/停止の安定化（PTY配下のプロセス管理）
- Windows Terminal 連携の実装コスト（補助機能の扱い）
- MAUI Blazor のウィンドウ分割/複数ウィンドウ管理の実装難易度
- ローカルサーバー/SQLite の同梱配布方法が未確定

## Notes / Logs
- 2026-01-29: WSL前提を撤回し、Windowsネイティブ（PowerShell）前提に変更。TTY必須のため ConPTY を採用し、Windows Terminal 連携は補助機能として検討する。
- 2026-01-29: UI を Svelte から MAUI Blazor へ切り替え。OSは Windows 11 前提、配布は EXE 生成までをスコープに変更。
- 2026-01-29: Codex 前提を撤回し、任意CLIコマンドを役割ごとに設定する方針に変更。
