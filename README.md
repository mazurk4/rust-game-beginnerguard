# Beginner Guard

An [Oxide/uMod](https://umod.org/) plugin for [Rust](https://store.steampowered.com/app/252490/Rust/) that keeps beginner servers beginner-friendly.  
It checks each player's Steam Rust playtime on connect and removes anyone who has outgrown your server's skill level.

**Version:** 1.6.0 | **Author:** Mazurk4_ | **License:** [MIT](LICENSE)

[日本語版 README はこちら](README-JPN.md)

---

## Screenshots

![Chat warning — Steam playtime unavailable](docs/screenshots/chat-warning.png)

*Orange chat warning shown when Steam game details or playtime cannot be read*

---

## What It Does

When a player joins, the plugin queries the **Steam Web API** for their total Rust playtime:

| Situation | Result |
|-----------|--------|
| Hours ≤ limit, playtime available | Allowed in |
| Hours > limit | Chat warning → kicked after delay |
| Game details/playtime unavailable, within grace period | Chat warning + kick scheduled at grace expiry |
| Playtime unavailable, over grace (warnings remaining) | Warning kick (counter +1) |
| Playtime unavailable, over grace (warnings exhausted) | Temporary BAN issued |
| Reconnecting while BAN'd | Instant kick showing time remaining |

All chat warnings are shown in **orange** and support multiple languages.  
Players are also **periodically re-checked** while they are online.

---

## Requirements

- [Oxide/uMod](https://umod.org/) installed on your Rust server
- A free **Steam Web API key** — get one at https://steamcommunity.com/dev/apikey

---

## Quick Start

```
1. Copy BeginnerGuard.cs  →  oxide/plugins/
2. oxide.reload BeginnerGuard
3. Edit oxide/config/BeginnerGuard.json  →  set "Steam API Key"
4. oxide.reload BeginnerGuard
```

---

## Features

- **Playtime gate** — configurable hour cap; warns then kicks over-limit players
- **Unavailable playtime handling** — grace period → warning kicks → temporary BAN
- **Automatic BAN expiry** — bans lift themselves; no admin action needed
- **Exempt permission** — whitelist VIPs, staff, and trusted players
- **Periodic re-check** — re-validates all online players on a schedule
- **Colored chat warnings** — orange `#FFA500` for easy visibility
- **Multi-language** — English and Japanese built-in; add more via `oxide/lang/`
- **Configurable save mode** — immediate save (default) or deferred periodic save to reduce disk IO on high-population servers
- **Stale record pruning** — automatically removes records of players who haven't connected in a configurable number of days (default: 90), keeping the data file lean
- **Discord webhook notifications** — independently enable notifications for grace, kick, BAN, reconnect, and unban stages

---

## Configuration

File: `oxide/config/BeginnerGuard.json`  
See [`config/BeginnerGuard.json.example`](config/BeginnerGuard.json.example) for a ready-to-use template.

| Setting | Default | Description |
|---------|---------|-------------|
| `Steam API Key` | *(required)* | Your Steam Web API key |
| `Max allowed Rust playtime on Steam (hours)` | `1000` | Players above this are kicked |
| `Private profile: max cumulative server playtime before kick (minutes)` | `120` | Total server time allowed when a player's Steam playtime is unavailable |
| `Steam API periodic check interval (seconds)` | `1800` | How often online players are re-checked (default: 30 min) |
| `Steam API retry interval on failure (seconds)` | `1800` | Retry delay when Steam API is unreachable |
| `Over-limit player: delay before kick after warning (seconds)` | `300` | Seconds between chat warning and kick |
| `Private profile: delay before kick after warning (seconds)` | `300` | Seconds between chat warning and kick |
| `Private profile: max warning kicks before BAN` | `2` | Warning kick count before a BAN is issued |
| `BAN duration (seconds)` | `86400` | How long the BAN lasts (default: 24 h) |
| `Private profile BAN grace` | See below | Optionally recheck visibility after a BAN and escalate if playtime is still unavailable |
| `Skip checks for Oxide admins` | `true` | Automatically exempt server admins |
| `Enable debug logging` | `false` | Print verbose logs to the server console |
| `Deferred data save` | `false` | `false` = save on every change (default); `true` = batch writes on a timer (reduces disk IO on busy servers) |
| `Data save interval (seconds)` | `300` | How often deferred saves are flushed to disk — only used when `Deferred data save` is `true` |
| `Stale record prune age (days, 0 = disabled)` | `90` | Player records older than this are removed automatically on startup; `0` disables pruning |
| `Discord webhook notifications` | See below | Discord Webhook URL, display name, and per-stage notification switches |

### Discord webhook notifications

Set `Webhook URL` under `Discord webhook notifications`, then enable only the events you need. Every notification is disabled by default.

| Setting | Notification timing |
|---------|---------------------|
| `Notify when private-profile grace period starts` | A player enters grace and a grace-expiry kick is scheduled |
| `Notify when private-profile grace period expires and player is kicked` | The grace period expires and the player is actually kicked |
| `Notify when private-profile warning kick occurs` | A post-grace warning kick is actually performed |
| `Notify when temporary BAN is issued` | An initial or escalated temporary BAN is issued |
| `Notify when a banned reconnect is blocked` | A reconnect attempt is blocked during an active BAN |
| `Notify when a BAN expires automatically` | BAN expiry is detected on connection; post-BAN grace then rechecks visibility when enabled |
| `Notify when bg.unban is used` | An administrator runs `bg.unban` |
| `Notify when an over-limit player is kicked` | A player is kicked for exceeding the Steam playtime limit |

Treat the Webhook URL as a secret and never place it in a public repository or log. Discord mentions are disabled in notification payloads.

### Post-BAN grace and escalation

`Private profile BAN grace` is disabled by default. When enabled, the plugin rechecks Steam visibility after the initial BAN expires.

```json
"BAN duration (seconds)": 3600.0,
"Private profile BAN grace": {
  "Enabled (recheck visibility after BAN expires)": true,
  "Escalated BAN duration (seconds)": 86400.0
}
```

- Visible playtime resets the BAN stage and warning count.
- Unavailable playtime immediately triggers the escalated BAN (24 hours in the example).
- If playtime remains unavailable after that BAN, the escalated duration repeats.
- BANs created before the option was enabled enter the initial stage on the player's next connection.
- Visible but over-limit playtime still follows the normal over-limit kick flow.
- `bg.unban` and `bg.reset` clear the BAN stage.

For Steam playtime to be available, the player must make Steam **Game details** public and disable the option that keeps total playtime private. A zero playtime value is treated as unavailable because Steam can return `0` when total playtime is hidden; genuinely new players remain protected by the normal grace period. Steam API failures are handled fail-open to avoid false kicks; the plugin keeps the player connected and retries later.

---

## Permissions

| Permission | Effect |
|------------|--------|
| `beginnerguard.exempt` | Skip all checks — for VIPs and trusted players |
| `beginnerguard.admin` | Use `bg.*` commands from the in-game F1 console |

```
oxide.grant group  <group>      beginnerguard.exempt
oxide.grant group  <group>      beginnerguard.admin
oxide.grant user   <SteamID64>  beginnerguard.exempt
```

---

## Commands

Available from the **server console / RCON** without any permissions.  
Requires `beginnerguard.admin` when used from the **in-game F1 console**.

| Command | Description |
|---------|-------------|
| `bg.help` | Show command list |
| `bg.check <SteamID64>` | Display a player's stored record |
| `bg.unban <SteamID64>` | Lift an active BAN |
| `bg.forcecheck <SteamID64>` | Trigger an immediate Steam API check (player must be online) |
| `bg.reset <SteamID64>` | Wipe all stored data for a player |
| `bg.prune` | Immediately remove stale records older than the configured prune age |
| `bg.debug <on\|off>` | Toggle debug logging without restarting |

### Re-evaluation after `bg.unban`

`bg.unban` immediately clears the BAN expiry, BAN stage, and warning-kick count. It does not clear cumulative server playtime or the last Steam result, and the command itself does not start a Steam API check.

- If the player is offline, the normal Steam API check runs on their next connection.
- If playtime is visible and within the limit, the player is allowed.
- If playtime remains unavailable and cumulative server time is already over the grace limit, enforcement resumes at warning count `0`.
- If the player is online and should be checked immediately, run `bg.forcecheck <SteamID64>` afterward.
- To also clear cumulative server playtime and all other stored state, use `bg.reset <SteamID64>` instead.

---

## How It Works

```
Player connects
    │
    ├─ Exempt (admin / beginnerguard.exempt)?  → Allow
    ├─ Currently BAN'd?                     → Kick (shows time remaining)
    │
    └─ Fetch Steam API
           │
           ├─ Game details/playtime unavailable
           │       ├─ Within grace period?        → Chat warning + kick scheduled at expiry
           │       ├─ Over grace, warnings left?   → Warning kick (counter +1)
           │       └─ Over grace, warnings used up? → BAN issued
           │              └─ BAN grace enabled, still unavailable after expiry? → Escalated BAN
           │
           ├─ API error → keep connected and retry
           │
           └─ Playtime available
                   ├─ Hours ≤ limit? → Allow
                   └─ Hours > limit? → Chat warning + kick after delay
```

---

## Localization

Language files are auto-generated in `oxide/lang/{code}/BeginnerGuard.json` on first load.

| Language | Code | Status |
|----------|------|--------|
| English  | `en` | Default |
| Japanese | `ja` | Built-in |
| Korean | `ko` | Deployment sample included |
| Simplified Chinese | `zh-CN` | Deployment sample included |
| Russian | `ru` | Deployment sample included |
| Vietnamese | `vi` | Deployment sample included |

**To add a new language:**

1. For an included sample, copy `lang/<code>/BeginnerGuard.json` to the server's `oxide/lang/<code>/BeginnerGuard.json`
2. Otherwise copy `oxide/lang/en/BeginnerGuard.json` and translate the values — **do not change the keys**
3. `oxide.reload BeginnerGuard`

See [`lang/en/BeginnerGuard.json`](lang/en/BeginnerGuard.json) for the full message list and placeholder reference.

---

## Data Storage

Records are saved to `oxide/data/BeginnerGuard.json` and persist across server restarts.  
Each record stores: Steam hours · profile visibility · cumulative server playtime · kick count · BAN stage · BAN expiry · last seen timestamp.

**Save modes** (configurable):
- **Immediate** (default) — data is written to disk on every change. Safe for small servers.
- **Deferred** — changes are batched and flushed on a periodic timer (`Data save interval`). Reduces disk IO on busy servers. BAN issuance and expiry are always written immediately regardless of this setting.

**Stale record pruning** — on server startup, records of players who have not connected for more than `Stale record prune age` days are deleted automatically. Players who are currently online or still banned are never pruned. Legacy records without a last-seen timestamp receive a migration timestamp and become eligible after one full retention period.

Temporary BANs are plugin-level restrictions: BeginnerGuard stores an expiry and kicks the player when they reconnect. They are not added to Rust's native ban list and do not apply while the plugin is disabled.

---

## Development Compile Check

With the .NET 8 SDK installed, you can check C# syntax and the shape of the API calls used by the plugin without starting a Rust server:

```bash
dotnet restore tests/compile/BeginnerGuard.CompileCheck.csproj \
  --configfile tests/compile/NuGet.Config
dotnet build tests/compile/BeginnerGuard.CompileCheck.csproj --no-restore
```

`tests/compile/UmodStubs.cs` contains minimal compile-only definitions for the Rust, uMod, and Newtonsoft.Json APIs. The check uses no external packages, Steam API key, or network access. It does not guarantee full compatibility or in-game behaviour, so changes must still be tested on a local Rust server before release.

### Credential Safety

This is a public repository. Never commit Steam API keys, RCON passwords, server tokens, webhook URLs, or any other credentials.

- Keep the placeholder in [`config/BeginnerGuard.json.example`](config/BeginnerGuard.json.example); never replace it with a real key
- Store the real key only in the Rust server's `oxide/config/BeginnerGuard.json`
- Review staged changes with `git diff --cached` before every commit
- If a credential is added accidentally, revoke and rotate it even if it has not been intentionally published

---

## Contributing

Bug reports, feature suggestions, and translation PRs are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).

---

## License

[MIT](LICENSE) — Copyright (C) 2024 Mazurk4_
