# MonochromeMemory.CodexMultiAgent

Windows ネイティブ（MAUI Blazor）で動作する、CLI コーディングエージェント向けのマルチエージェント基盤。
役割は `config/roles.yaml` と `roles/*.md` で定義し、任意コマンドでエージェントを起動できます。

## 前提
- Windows 11
- .NET 8
- MAUI テンプレート（`dotnet new install Microsoft.Maui.Templates`）

## 起動（開発）
```powershell
./scripts/start.ps1
```

## 役割の定義
- 役割一覧: `config/roles.yaml`
- 役割の指示書: `roles/*.md`

`roles.yaml` の `command` で起動コマンドを指定できます。

```yaml
roles:
  - id: ashigaru1
    name: "足軽1"
    prompt_path: "roles/ashigaru1.md"
    enabled: true
    command:
      exec: "gemini"
      args: []
      env: {}
      cwd: "."
```

## ランタイム構成
- タスク: `runtime/queue/tasks/*.yaml`
- 家老指示: `runtime/queue/commands/karo.yaml`
- 報告: `runtime/queue/reports/*.yaml`
- ログ: `runtime/logs/`

## スキル
- 正本: `skills/registry/<skill-name>/SKILL.md`
- 連携先: `~/.codex/skills/` と `~/.copilot/skills/`（シンボリックリンク、不可ならコピー）
- 形式: Markdown + Front Matter

```markdown
---
name: skill-name
description: このスキルの説明（いつ使うか、何をするか）
---

# 本文
ここに実際の指示を書く
```

## 注意
- TTY 必須の CLI を想定しています。
- Windows Terminal 連携は任意機能です（`scripts/terminal.ps1` を編集）。
