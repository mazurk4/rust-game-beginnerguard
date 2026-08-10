# Beginner Guard

[Oxide/uMod](https://umod.org/) 向け [Rust](https://store.steampowered.com/app/252490/Rust/) プラグインです。  
接続してきたプレイヤーの Steam Rust プレイ時間を自動確認し、初心者サーバーを守ります。

**バージョン:** 1.8.0 | **作者:** Mazurk4_ | **ライセンス:** [MIT](LICENSE)

---

## スクリーンショット

![チャット警告 — Steamプレイ時間を取得不能](docs/screenshots/chat-warning.png)

*Steamのゲーム詳細またはプレイ時間を確認できない場合に表示されるチャット警告（オレンジ色）*

---

## 何をするプラグイン？

プレイヤーが接続すると、**Steam Web API** でその人の Rust 総プレイ時間を取得します。

| 状況 | 結果 |
|------|------|
| 時間数 ≤ 上限、プレイ時間を取得可能 | そのまま入場 |
| 時間数 > 上限 | チャット警告 → 一定時間後にキック |
| ゲーム詳細・プレイ時間を取得不能、グレース期間内 | チャット警告 + グレース満了時にキック |
| 取得不能、グレース超過（警告回数残あり） | 警告キック（カウント +1） |
| 取得不能、警告回数使い果たし | 一時BAN発行 |
| BAN中に再接続 | 残り時間を表示して即キック |

チャット警告は**オレンジ色**で表示され、多言語に対応しています。  
オンライン中のプレイヤーは**定期的に再チェック**されます。

---

## 必要要件

- Rust サーバーへの [Oxide/uMod](https://umod.org/) のインストール
- 無料の **Steam Web API キー** — 取得先: https://steamcommunity.com/dev/apikey

---

## クイックスタート

```
1. BeginnerGuard.cs を  oxide/plugins/  にコピー
2. oxide.reload BeginnerGuard
3. oxide/config/BeginnerGuard.json を開いて "Steam API Key" を設定
4. oxide.reload BeginnerGuard
```

---

## 機能

- **プレイ時間ゲート** — 時間上限を超えたプレイヤーを警告後にキック
- **プレイ時間を取得できない場合の対応** — グレース期間 → 警告キック → 一時BAN の段階的処理
- **BAN 自動解除** — 時間が来ると自動解除。手動作業不要
- **免除パーミッション** — VIP・スタッフ・信頼済みプレイヤーをチェック対象外にできる
- **定期再チェック** — 設定間隔でオンライン全プレイヤーを再検証
- **プレイヤー自身による確認** — `/bgstatus` で、Steam APIへ追加リクエストせずに最新の保存済み参加判定を確認可能
- **色付きチャット警告** — オレンジ色（`#FFA500`）で見やすく表示
- **多言語対応** — 英語・日本語標準搭載。`oxide/lang/` に追加するだけで他言語も対応可能
- **保存モード切り替え** — 即時保存（デフォルト）と定期保存（遅延書き込み）を設定で選択可能。大規模サーバーのディスク IO 削減に有効
- **古いレコードの自動削除** — 設定日数（デフォルト90日）以上接続のないプレイヤーのレコードを起動時に自動削除し、データファイルの肥大化を防ぐ
- **Discord Webhook 通知** — グレース開始・各種キック・BAN発行/解除など、通知する段階を個別に設定可能

---

## 設定

ファイル: `oxide/config/BeginnerGuard.json`  
テンプレートは [`config/BeginnerGuard.json.example`](config/BeginnerGuard.json.example) を参照してください。

| キー | デフォルト | 説明 |
|------|-----------|------|
| `Steam API Key` | *(必須)* | Steam Web API キー |
| `Max allowed Rust playtime on Steam (hours)` | `1000` | この時間数を超えるとキック対象 |
| `Private profile: max cumulative server playtime before kick (minutes)` | `120` | プレイ時間を取得できないプレイヤーに許容するサーバー累積滞在時間 |
| `Steam API periodic check interval (seconds)` | `1800` | オンラインプレイヤーの再チェック間隔（デフォルト: 30分） |
| `Steam API retry interval on failure (seconds)` | `1800` | API エラー時の再試行間隔 |
| `Over-limit player: delay before kick after warning (seconds)` | `300` | 警告からキックまでの待機時間（プレイ時間超過） |
| `Private profile: delay before kick after warning (seconds)` | `300` | 警告からキックまでの待機時間（プレイ時間を取得不能） |
| `Private profile: max warning kicks before BAN` | `2` | BAN に移行するまでの警告キック回数 |
| `BAN duration (seconds)` | `86400` | BAN の長さ（デフォルト: 24時間） |
| `Private profile BAN grace` | 下記参照 | BAN終了後に公開状態を再確認し、未公開なら長時間BANへ昇格するオプション |
| `Skip checks for Oxide admins` | `true` | Oxide 管理者を自動で免除する |
| `Enable debug logging` | `false` | サーバーコンソールに詳細ログを出力する |
| `Deferred data save` | `false` | `false` = 変更のたびに即時保存（デフォルト）、`true` = タイマーによる定期保存（大規模サーバー向け） |
| `Data save interval (seconds)` | `300` | 定期保存の間隔（秒）— `Deferred data save` が `true` のときのみ有効 |
| `Stale record prune age (days, 0 = disabled)` | `90` | この日数以上接続のないプレイヤーのレコードを起動時に自動削除。`0` で無効 |
| `Discord webhook notifications` | 下記参照 | Discord Webhook URL、表示名、段階別通知スイッチ |

### Discord Webhook 通知

`Discord webhook notifications` の `Webhook URL` に Discord の Webhook URL を設定し、必要な通知だけ `true` にします。デフォルトでは全通知が無効です。

| 通知設定 | 通知タイミング |
|----------|----------------|
| `Notify when private-profile grace period starts` | プレイ時間を取得できず、グレース満了キックを予約した時 |
| `Notify when private-profile grace period expires and player is kicked` | グレースが満了し、実際にキックした時 |
| `Notify when private-profile warning kick occurs` | グレース超過後の警告キックを実行した時 |
| `Notify when temporary BAN is issued` | 初回または昇格の一時BANを発行した時 |
| `Notify when a banned reconnect is blocked` | BAN中の再接続を拒否した時 |
| `Notify when a BAN expires automatically` | 接続時にBAN期限切れを検出した時（BANグレース有効時は続けて公開状態を再確認） |
| `Notify when bg.unban is used` | 管理者が `bg.unban` を実行した時 |
| `Notify when an over-limit player is kicked` | Steamプレイ時間上限超過によるキックを実行した時 |

Webhook URL は秘密情報として扱い、公開リポジトリやログに貼らないでください。通知本文では Discord のメンションを無効化しています。

### BANグレース（段階的BAN）

`Private profile BAN grace` はデフォルトで無効です。有効にすると、初回BANの終了後にSteamの公開状態を再確認します。

```json
"BAN duration (seconds)": 3600.0,
"Private profile BAN grace": {
  "Enabled (recheck visibility after BAN expires)": true,
  "Escalated BAN duration (seconds)": 86400.0
}
```

- 初回BAN終了後、総プレイ時間を確認できればBAN段階と警告回数をリセットします。
- まだ確認できなければ、即座に昇格BAN（上記例では24時間）を適用します。
- 昇格BAN終了後も確認できない場合は、同じ昇格BANを繰り返します。
- 機能を有効化する前から存在するBANも、次回接続時に初回BAN段階として引き継ぎます。
- Steam時間が公開されていても上限を超えている場合は、通常の時間超過キックを適用します。
- `bg.unban` と `bg.reset` はBAN段階もリセットします。

Steam APIから時間を取得するには、プレイヤー側でSteamの「ゲームの詳細」を公開し、総プレイ時間を非公開にする設定をオフにする必要があります。Steamは総プレイ時間が非公開の場合に `0` を返すことがあるため、0時間は取得不能として扱います。本当に初回起動のプレイヤーには通常のグレース期間が適用されます。Steam API障害時は誤判定によるキックを避けるため入場を維持し、設定間隔で再試行します。

---

## パーミッション

| パーミッション | 効果 |
|----------------|------|
| `beginnerguard.exempt` | 全チェックをスキップ（VIP・スタッフ向け） |
| `beginnerguard.admin` | ゲーム内 F1 コンソールから `bg.*` コマンドを使用可能 |
| `beginnerguard.status` | チャットで `/bgstatus` と `/bgstatus steam` を使用可能 |

```
oxide.grant group  <グループ名>  beginnerguard.exempt
oxide.grant group  <グループ名>  beginnerguard.admin
oxide.grant user   <SteamID64>  beginnerguard.exempt
oxide.grant group  default      beginnerguard.status
oxide.grant user   <SteamID64>  beginnerguard.status
```

全プレイヤーに許可する場合は、`default` グループへ `beginnerguard.status` を付与します。特定プレイヤーだけに許可する場合は、SteamID64を指定して個別に付与してください。取り消す場合は `oxide.revoke group default beginnerguard.status` または対応する `oxide.revoke user` コマンドを使用します。

---

## コマンド

`beginnerguard.status` を持つプレイヤーは、次のチャットコマンドを使用できます。どちらもSteam APIへリクエストしないため、繰り返し実行してもAPI制限に影響しません。

| チャットコマンド | 説明 |
|------------------|------|
| `/bgstatus` | 自分のSteam公開状態とプレイ時間に対する最新の保存済み判定を表示。非公開・確認不能の場合は `/bgstatus steam` を案内します。 |
| `/bgstatus steam` | Steamのゲーム詳細と総プレイ時間を公開する手順を表示します。 |

### 管理コマンド

**サーバーコンソール / RCON** からはパーミッションなしで使用できます。  
**ゲーム内 F1 コンソール**から使用するには `beginnerguard.admin` が必要です。

| コマンド | 説明 |
|---------|------|
| `bg.help` | コマンド一覧を表示 |
| `bg.list [ページ]` | 管理対象プレイヤーを一覧表示（1ページ20件） |
| `bg.list online [ページ]` | 現在オンラインの管理対象プレイヤーを一覧表示 |
| `bg.banlist [ページ]` | BeginnerGuardによる有効なBANを一覧表示 |
| `bg.check <SteamID64>` | プレイヤーの保存データを表示 |
| `bg.unban <SteamID64>` | アクティブな BAN を解除 |
| `bg.forcecheck <SteamID64>` | Steam API チェックを即時実行（オンライン中のみ） |
| `bg.reset <SteamID64>` | プレイヤーの保存データを全リセット |
| `bg.prune` | 設定の保持日数を超えた古いレコードを即時削除 |
| `bg.debug <on\|off>` | リロードなしでデバッグログのオン/オフ切替 |

一覧にはオンライン状態、判定状態（`ALLOWED` / `OVER_LIMIT` / `UNAVAILABLE` /
`UNCHECKED` / `BANNED`）、名前、SteamID64、Steamプレイ時間、現在のセッションを
含むサーバー累積滞在時間、警告回数、BAN期限（UTC）が表示されます。

### `bg.unban` 後の再判定

`bg.unban` は BAN 期限、BAN段階、警告キック回数を即時にリセットしますが、累積サーバー滞在時間や直近の Steam 判定結果は消去しません。また、コマンド実行だけでは Steam API の再判定を開始しません。

- 対象がオフラインなら、次回接続時に通常どおり Steam API で再判定します。
- プレイ時間が取得でき、上限以内なら入場できます。
- プレイ時間を取得できないままで、累積滞在時間が既にグレース上限以上なら、警告回数 `0` から警告キック段階を再開します。
- オンライン中に解除して即時再判定したい場合は、続けて `bg.forcecheck <SteamID64>` を実行します。
- 累積滞在時間を含めて完全に初期化する場合は、代わりに `bg.reset <SteamID64>` を使用します。

---

## 動作フロー

```
プレイヤー接続
    │
    ├─ 免除対象（管理者 / beginnerguard.exempt）?  → 入場
    ├─ BAN 中?                                  → キック（残り時間を表示）
    │
    └─ Steam API でプレイ時間を取得
           │
           ├─ ゲーム詳細・プレイ時間を取得不能
           │       ├─ グレース期間内?              → チャット警告 + 満了時にキック
           │       ├─ グレース超過、警告回数残あり?  → 警告キック（カウント +1）
           │       └─ グレース超過、警告回数ゼロ?   → BAN 発行
           │              └─ BANグレース有効、期限後も取得不能? → 昇格BAN
           │
           ├─ API エラー → 入場を維持して再試行
           │
           └─ プレイ時間を取得
                   ├─ 時間数 ≤ 上限? → 入場
                   └─ 時間数 > 上限? → チャット警告 + 遅延後キック
```

---

## 多言語対応

言語ファイルは初回起動時に `oxide/lang/{言語コード}/BeginnerGuard.json` へ**自動生成**されます。

| 言語 | コード | 状態 |
|------|--------|------|
| English | `en` | デフォルト |
| 日本語 | `ja` | 標準搭載 |
| 한국어 | `ko` | 配置用サンプルあり |
| 简体中文 | `zh-CN` | 配置用サンプルあり |
| Русский | `ru` | 配置用サンプルあり |
| Tiếng Việt | `vi` | 配置用サンプルあり |

**新しい言語を追加するには:**

1. サンプルがある言語は `lang/<コード>/BeginnerGuard.json` をサーバーの `oxide/lang/<コード>/BeginnerGuard.json` にコピー
2. それ以外は `oxide/lang/en/BeginnerGuard.json` をコピーして値を翻訳する — **キーは変更しないこと**
3. `oxide.reload BeginnerGuard`

メッセージ一覧とプレースホルダーの詳細は [`lang/en/BeginnerGuard.json`](lang/en/BeginnerGuard.json) を参照してください。

---

## データ保存

プレイヤーのデータは `oxide/data/BeginnerGuard.json` に保存され、サーバー再起動後も引き継がれます。  
記録内容: Steam時間数 · プロフィール公開状態 · サーバー累積滞在時間 · 警告キック回数 · BAN段階 · BAN解除時刻 · 最終接続日時

**保存モード**（設定で切り替え可能）:
- **即時保存**（デフォルト）— 変更のたびにディスクへ書き込む。小規模サーバー向け。
- **定期保存** — 変更をまとめてタイマー間隔で書き込む。大規模サーバーのディスク IO 削減に有効。BAN の発行・解除は保存モードに関係なく常に即時書き込み。

**古いレコードの自動削除** — サーバー起動時に `Stale record prune age` 日数を超えた未接続プレイヤーのレコードを自動削除します。オンライン中またはBAN中のプレイヤーは対象外です。最終接続日時がない旧レコードには移行時刻を設定し、そこから保持期間が経過した後に削除対象とします。

一時BANはRust本体のBANリストではなく、このプラグインが期限を保存して再接続時にキックする仕組みです。プラグインを無効化している間は適用されません。

---

## 開発時の仮コンパイル

.NET 8 SDKがあれば、Rustサーバーを起動せずにC#の構文と、プラグインが利用するAPI呼び出しの形を確認できます。

```bash
dotnet restore tests/compile/BeginnerGuard.CompileCheck.csproj \
  --configfile tests/compile/NuGet.Config
dotnet build tests/compile/BeginnerGuard.CompileCheck.csproj --no-restore
```

`tests/compile/UmodStubs.cs` はRust・uMod・Newtonsoft.Json APIの最小限の仮定義です。外部パッケージやSteam APIキーを使わず、ネットワークアクセスも必要ありません。この確認は実際のuMod環境との完全な互換性やゲーム内動作を保証しないため、リリース前にはローカルRustサーバーでも確認してください。

### クレデンシャルの取り扱い

このリポジトリは公開されています。Steam APIキー、RCONパスワード、サーバートークン、Webhook URLなどのクレデンシャルは絶対にコミットしないでください。

- [`config/BeginnerGuard.json.example`](config/BeginnerGuard.json.example) のプレースホルダーを実キーへ置き換えない
- 実キーはRustサーバー上の `oxide/config/BeginnerGuard.json` にだけ設定する
- コミット前に `git diff --cached` でステージ済み差分を確認する
- 誤って追加した場合は、公開前でもキーを失効・再発行する

---

## コントリビューション（貢献）

バグ報告・機能提案・翻訳など、PR は歓迎します。  
詳細は [CONTRIBUTING.md](CONTRIBUTING.md) を参照してください。

---

## ライセンス

[MIT](LICENSE) — Copyright (C) 2024 Mazurk4_
