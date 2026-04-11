# BeginnerGuard

[Oxide/uMod](https://umod.org/) 向け [Rust](https://store.steampowered.com/app/252490/Rust/) プラグインです。  
Steam Web API でプレイヤーの Rust プレイ時間を確認し、初心者サーバーを保護します。

**バージョン:** 1.4.0 | **作者:** Mazurk4_

---

## 機能

- 接続時に Steam API でプレイヤーの Rust プレイ時間を自動取得
- **プレイ時間超過キック** — 設定した上限を超えるプレイヤーをチャット警告後にキック
- **プロフィール非公開対応** — グレース期間 → 警告キック → 時間制限付きBAN の段階的処理
- **BAN 自動解除** — 設定した時間が過ぎると自動でBANが解除される
- **免除パーミッション** — VIP やスタッフを `beginguard.exempt` でチェック対象外にできる
- **定期再チェック** — 設定した間隔でオンライン全プレイヤーを再検証
- **チャットの色付き警告** — ゲーム内警告はオレンジ色（`#FFA500`）で表示
- **多言語対応** — 英語・日本語標準搭載。`oxide/lang/` に追加するだけで他言語にも対応可能

---

## 必要要件

- Rust サーバーへの [Oxide/uMod](https://umod.org/) のインストール
- **Steam Web API キー** — 無料で取得可能: https://steamcommunity.com/dev/apikey

---

## インストール手順

1. `BeginnerGuard.cs` を `oxide/plugins/` にコピー
2. サーバーを再起動するか `oxide.reload BeginnerGuard` を実行
3. 生成された `oxide/config/BeginnerGuard.json` を開き Steam API キーを設定
4. 再度リロード: `oxide.reload BeginnerGuard`

---

## 設定

ファイル: `oxide/config/BeginnerGuard.json`

テンプレートは [`config/BeginnerGuard.json.example`](config/BeginnerGuard.json.example) を参照してください。

| キー | 型 | デフォルト | 説明 |
|------|----|-----------|------|
| `Steam API Key` | string | `"YOUR_STEAM_API_KEY_HERE"` | Steam Web API キー（必須） |
| `Max allowed Rust playtime on Steam (hours)` | int | `1000` | これを超えるとキック対象 |
| `Private profile: max cumulative server playtime before kick (minutes)` | int | `120` | 非公開プロフィールに許容するサーバー累積滞在時間 |
| `Steam API periodic check interval (seconds)` | float | `1800` | オンラインプレイヤーの再チェック間隔 |
| `Steam API retry interval on failure (seconds)` | float | `1800` | API エラー時の再試行間隔 |
| `Over-limit player: delay before kick after warning (seconds)` | float | `300` | 警告からキックまでの待機時間（プレイ時間超過） |
| `Private profile: delay before kick after warning (seconds)` | float | `300` | 警告からキックまでの待機時間（非公開プロフィール） |
| `Private profile: max warning kicks before BAN` | int | `2` | BAN に移行するまでの警告キック回数 |
| `BAN duration (seconds)` | float | `86400` | BAN の長さ（デフォルト: 24時間） |
| `Skip checks for Oxide admins` | bool | `true` | Oxide 管理者を自動で免除する |
| `Enable debug logging` | bool | `false` | サーバーコンソールに詳細ログを出力する |

---

## パーミッション

| パーミッション | 効果 |
|----------------|------|
| `beginguard.exempt` | 全チェックをスキップ（VIP・スタッフ向け） |
| `beginguard.admin` | ゲーム内コンソールから `bg.*` コマンドを使用可能 |

```
# グループに付与
oxide.grant group <グループ名> beginguard.exempt
oxide.grant group <グループ名> beginguard.admin

# 特定プレイヤーに付与
oxide.grant user <SteamID64> beginguard.exempt
```

---

## コンソールコマンド

サーバーコンソール・RCON からはパーミッションなしで使用可能です。  
ゲーム内コンソールから使用するには `beginguard.admin` パーミッションが必要です。

| コマンド | 説明 |
|---------|------|
| `bg.help` | コマンド一覧を表示 |
| `bg.check <SteamID64>` | プレイヤーの保存データを表示 |
| `bg.unban <SteamID64>` | アクティブな BAN を解除 |
| `bg.forcecheck <SteamID64>` | Steam API チェックを即時実行（オンライン中のみ） |
| `bg.reset <SteamID64>` | プレイヤーの保存データを全リセット |
| `bg.debug <on\|off>` | デバッグログのオン/オフ切替 |

---

## 多言語対応

言語ファイルは `oxide/lang/{言語コード}/BeginnerGuard.json` に保存されます。  
プラグイン初回起動時に**自動生成**されます。

| 言語 | コード | 状態 |
|------|--------|------|
| English | `en` | 標準搭載（デフォルト） |
| 日本語 | `ja` | 標準搭載 |

英語ファイルの例: [`lang/en/BeginnerGuard.json`](lang/en/BeginnerGuard.json)

### 新しい言語を追加する方法

1. `oxide/lang/en/BeginnerGuard.json` を `oxide/lang/<コード>/BeginnerGuard.json` にコピー  
   （例: 中国語なら `oxide/lang/zh/BeginnerGuard.json`）
2. **キーはそのまま**にして、値（メッセージ本文）を翻訳する
3. `oxide.reload BeginnerGuard` を実行

### メッセージのプレースホルダー

| キー | `{0}` | `{1}` | `{2}` | `{3}` |
|-----|-------|-------|-------|-------|
| `PrivateProfile.GraceWarn` | 残り時間（分） | — | — | — |
| `PrivateProfile.WarnKick` | キックまでの秒数 | 現在の警告回数 | 最大警告回数 | BAN時間（時間） |
| `PrivateProfile.BanKickReason` | BAN時間（時間） | — | — | — |
| `PrivateProfile.BanConnectKick` | 残り時間（時間） | 残り時間（分） | — | — |
| `OverLimit.Warn` | プレイヤーの時間数 | 上限時間数 | キックまでの秒数 | — |
| `OverLimit.KickReason` | プレイヤーの時間数 | 上限時間数 | — | — |

---

## 動作フロー

```
プレイヤー接続
    │
    ├─ 免除対象（管理者 / beginguard.exempt）? → 通過
    │
    ├─ BAN 中? → 残り時間を表示してキック
    │
    └─ Steam API でプレイ時間を取得
           │
           ├─ プロフィール非公開 / API エラー
           │       ├─ グレース期間内? → チャット警告 → 期限到達時にキック
           │       ├─ グレース超過、警告回数 < 上限? → 警告キック（カウント増加）
           │       └─ グレース超過、警告回数 ≥ 上限? → BAN 発行
           │
           └─ プロフィール公開
                   ├─ 時間数 ≤ 上限? → 通過
                   └─ 時間数 > 上限? → チャット警告 → 遅延後キック
```

---

## データ保存

プレイヤーのデータは `oxide/data/BeginnerGuard.json` に保存され、サーバー再起動後も引き継がれます。  
記録内容: Steam時間数・プロフィール公開状態・サーバー累積滞在時間・警告キック回数・BAN解除時刻

---

## コントリビューション（貢献）

バグ報告・機能提案・翻訳など、PRは歓迎します。  
提出前に必ず実際のサーバーで動作確認をしてください。  
詳細は [CONTRIBUTING.md](CONTRIBUTING.md) を参照してください。

---

## ライセンス

[GPL v3](LICENSE) — Copyright (C) 2024 Mazurk4_

このプラグインは自由に使用・改変・再配布できますが、改変版を公開する場合は  
**GPL v3 ライセンスの継承**と**原作者クレジットの保持**が必要です。  
詳細は [LICENSE](LICENSE) を参照してください。
