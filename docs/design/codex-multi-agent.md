# CLIマルチエージェント基盤 設計（Windowsネイティブ / MAUI Blazor）

## 目的
- Windows（PowerShell）から操作可能なマルチエージェント基盤を構築する
- 任意のCLIエージェントを複数起動し、役割ごとに並列実行できる
- 役割定義を `config/roles.yaml` + 役割ファイルでユーザーが自由に追加/編集できる
- MAUI Blazor UI で「役割管理・起動/停止・タスク投入・ログ/進捗/履歴」を提供する
- データは SQLite に永続化する

## 非目的
- WSL/tmux 依存の運用
- 特定CLIの専用拡張に依存する運用
- クラウド配布やマルチユーザー運用

## 前提
- 対象CLIはTTY対応（TTY必須のケースを想定）
- 見えるターミナルが必要
- Windows 11 前提
- Windows Terminal 連携は任意（補助的機能）

## 全体アーキテクチャ
```mermaid
flowchart LR
  subgraph Windows
    UI[MAUI Blazor UI]
    ORCH[Orchestrator Service]
    DB[(SQLite)]
    CFG[config/roles.yaml + roles/*.md]
    PTY[ConPTY Manager]
    AG1[Agent 1]
    AGN[Agent N]
    QUEUE[queue/tasks/*.yaml]
    REPORT[queue/reports/*.yaml]
    LOGS[logs/*.log]
    SKILL[skills/*.md]
    WT[Windows Terminal (optional)]
  end

  UI <--> ORCH
  ORCH <--> DB
  ORCH <--> CFG
  ORCH <--> PTY
  PTY --> AG1
  PTY --> AGN
  ORCH <--> QUEUE
  ORCH <--> REPORT
  ORCH <--> LOGS
  ORCH <--> SKILL
  ORCH -. optional .-> WT
```

## コンポーネント

### 1) MAUI Blazor UI
- 役割管理（作成/編集/削除/有効化）
- 起動/停止（セッション管理）
- タスク投入（役割単位に割り当て）
- 進捗表示（タスク/役割の状態、最新ログ）
- 履歴表示（過去タスク、実行ログ）
- ターミナル表示（ConPTY 出力をウィンドウで表示）

### 2) Orchestrator Service（アプリ内）
- ConPTY を使って 役割ごとのコマンド を PTY 配下で起動
- 役割設定の読込・検証・永続化
- タスク作成、キュー生成、進捗/履歴管理
- SQLite への保存
- Windows Terminal 連携（任意）

### 3) ConPTY セッション管理
- 役割ごとに PTY セッションを作成
- UI へストリーム配信
- タスク投入時に PTY へ入力を書き込む
- ログを `runtime/logs/*.log` に保存

## ConPTY 実装方針（ベストプラクティス）
- ConPTY は Win32 API を直接呼び出す（P/Invoke）
- Windows Terminal のサンプル（GUIConsole.ConPTY / EchoCon）を参照し、以下の基本手順を踏襲する
  1. 入出力パイプを作成
  2. `CreatePseudoConsole` で PTY を作成
  3. `STARTUPINFOEX` + `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` で子プロセスを接続
  4. 出力パイプを非同期で読み続ける
- `PSEUDOCONSOLE_INHERIT_CURSOR` は原則使わない（使用時は非同期でカーソル応答が必要）
- `ClosePseudoConsole` 前後は出力パイプを閉じるか読み続け、デッドロックを回避する

## Windows Terminal 連携の位置づけ
- 主経路は MAUI UI のターミナル表示
- Windows Terminal 連携は補助的（監視や手動操作向け）
- 連携方式は `wt.exe` で新規タブを開き、ログを tail する等
- PTY と Windows Terminal の完全同期は対象外

## スキル自動生成（Codex/Copilot 対応）
### 目的
- タスク履歴から反復パターンを検出し、スキル候補を自動生成する
- ユーザー承認後に Codex/Copilot のスキル形式で出力する

### フロー
1. タスク履歴を SQLite に蓄積
2. 一定回数以上の反復を検出して `skill_candidates` を生成
3. UI で候補を承認/却下
4. 承認された候補をターゲット別にエクスポート（Codex/Copilot）

### 形式（ターゲット別）
**Codex / Copilot 共通の Markdown + Front Matter** を採用する。

```markdown
---
name: skill-name
description: このスキルの説明（いつ使うか、何をするか）
---

# 本文
ここに実際の指示を書く
```

### エクスポート先（例）
- Codex: `skills/codex/{skill-name}.md`
- Copilot: `skills/copilot/{skill-name}.md`

### 最小の検出ルール（初期）
- 同一ロールで同一タイトル/タグが一定回数（例: 2回以上）
- 類似度閾値（将来的に設定で調整可能）

## ディレクトリ構成（新規）
```
MonochromeMemory.CodexMultiAgent/
├─ app/                    # MAUI Blazor App
├─ config/
│  ├─ settings.yaml         # Windows/PTY/DB/UI 設定
│  └─ roles.yaml            # 役割一覧
├─ roles/                   # 役割ファイル（Markdown）
├─ skills/
│  ├─ codex/                # Codex向けスキル出力
│  └─ copilot/              # Copilot向けスキル出力
├─ runtime/
│  ├─ queue/
│  │  ├─ tasks/             # 役割別タスク（YAML）
│  │  ├─ commands/          # 家老→足軽 指示（YAML）
│  │  └─ reports/           # 役割別レポート（YAML）
│  └─ logs/                 # 役割別ログ
├─ scripts/
│  ├─ start.ps1             # PowerShell 起動スクリプト
│  └─ terminal.ps1          # Windows Terminal 連携（任意）
└─ README.md
```

## 役割定義
### roles.yaml（例）
```yaml
roles:
  - id: shogun
    name: "将軍"
    prompt_path: "roles/shogun.md"
    enabled: true
    command:
      exec: "codex"
      args: []
      env: {}
      cwd: "."
  - id: ashigaru1
    name: "足軽1"
    prompt_path: "roles/ashigaru1.md"
    enabled: true
    command:
      exec: "claude"
      args: ["--dangerously-skip-permissions"]
      env: {}
      cwd: "."
```

### 役割ファイル
- Markdown 形式
- 役割の目的・禁止事項・作業手順・報告フォーマットなどを記述

## SQLite スキーマ（最小構成）
```sql
-- 役割
CREATE TABLE roles (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  prompt_path TEXT NOT NULL,
  enabled INTEGER NOT NULL DEFAULT 1,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

-- 実行セッション
CREATE TABLE runs (
  id TEXT PRIMARY KEY,
  started_at TEXT NOT NULL,
  ended_at TEXT,
  status TEXT NOT NULL
);

-- タスク
CREATE TABLE tasks (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL,
  role_id TEXT NOT NULL,
  title TEXT NOT NULL,
  body TEXT NOT NULL,
  status TEXT NOT NULL,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  FOREIGN KEY(run_id) REFERENCES runs(id),
  FOREIGN KEY(role_id) REFERENCES roles(id)
);

-- ログ（メタ情報）
CREATE TABLE logs (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL,
  role_id TEXT NOT NULL,
  path TEXT NOT NULL,
  created_at TEXT NOT NULL,
  FOREIGN KEY(run_id) REFERENCES runs(id),
  FOREIGN KEY(role_id) REFERENCES roles(id)
);

-- スキル候補
CREATE TABLE skill_candidates (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  description TEXT NOT NULL,
  body TEXT NOT NULL,
  source_task_ids TEXT NOT NULL,
  status TEXT NOT NULL,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

-- スキル（エクスポート済み）
CREATE TABLE skills (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  description TEXT NOT NULL,
  body TEXT NOT NULL,
  target TEXT NOT NULL,
  path TEXT NOT NULL,
  created_at TEXT NOT NULL
);
```

## 主要フロー

### 1) 起動
1. PowerShell から `scripts/start.ps1` を実行
2. MAUI アプリ起動
3. 役割数に応じて PTY セッションを作成し、役割コマンドを起動
4. 役割指示書を読み込むよう初期入力

### 2) タスク投入
1. UI で役割とタスク内容を選択
2. SQLite に保存
3. `runtime/queue/tasks/<role>.yaml` を生成
4. PTY へ「タスクファイルを読め」と入力送信

### 2.1) 家老→足軽 指示フロー（YAML方式）
1. 家老は `runtime/queue/commands/karo.yaml` に指示を記入
2. Orchestrator が FileSystemWatcher で検知
3. Orchestrator が `runtime/queue/tasks/ashigaruX.yaml` を生成
4. 対象足軽の PTY へ「タスクファイルを読め」と入力送信

### 3) 進捗・ログ表示
- `runtime/logs/*.log` を UI から参照
- 必要に応じて `queue/reports/*.yaml` を読み込んで状態を更新

## 設定ファイル（settings.yaml 例）
```yaml
pty:
  backend: "conpty"

agent:
  default_command:
    exec: "codex"
    args: []
    env: {}
    cwd: "."

ui:
  host: "localhost"
  port: 5173

data:
  sqlite_path: "data/app.db"

terminal:
  windows_terminal:
    enabled: true

skills:
  enabled: true
  codex_path: "skills/codex"
  copilot_path: "skills/copilot"
  auto_candidate_threshold: 2
```

## 仕様上の制約
- PTY 管理が主経路であり、Windows Terminal 連携は補助
- CLIの起動オプションは固定せず、役割ファイルの初期指示で役割を浸透させる
- Windows 環境差異により PTY の互換性に注意が必要
