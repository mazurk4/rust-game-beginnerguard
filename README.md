# BeginnerGuard

An [Oxide/uMod](https://umod.org/) plugin for [Rust](https://store.steampowered.com/app/252490/Rust/) that protects beginner servers by checking players' Steam Rust playtime via the Steam Web API.

**Version:** 1.4.0 | **Author:** Mazurk4_

[日本語版 README はこちら](README-JPN.md)

---

## Features

- Fetches each player's Rust playtime from the Steam Web API on connect
- **Over-limit kick** — warns then kicks players who exceed the configured playtime cap
- **Private profile handling** — grace period + warning kicks before a time-limited BAN
- **Automatic BAN expiry** — bans lift automatically; no manual cleanup needed
- **Exempt permission** — whitelist individuals or groups (e.g. your admin team)
- **Periodic re-check** — re-validates all online players on a configurable interval
- **Colored chat messages** — in-game warnings rendered in orange (`#FFA500`)
- **Multi-language support** — English (default) and Japanese included; fully customizable via `oxide/lang/`

---

## Requirements

- [Oxide/uMod](https://umod.org/) installed on your Rust server
- A **Steam Web API key** — obtain one free at https://steamcommunity.com/dev/apikey

---

## Installation

1. Copy `BeginnerGuard.cs` to `oxide/plugins/`
2. (Re)start the server or run `oxide.reload BeginnerGuard`
3. Open `oxide/config/BeginnerGuard.json` and set your Steam API key
4. Reload again: `oxide.reload BeginnerGuard`

---

## Configuration

File: `oxide/config/BeginnerGuard.json`

See [`config/BeginnerGuard.json.example`](config/BeginnerGuard.json.example) for a ready-to-use template.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Steam API Key` | string | `"YOUR_STEAM_API_KEY_HERE"` | Steam Web API key |
| `Max allowed Rust playtime on Steam (hours)` | int | `1000` | Players above this are kicked |
| `Private profile: max cumulative server playtime before kick (minutes)` | int | `120` | Grace period for private profiles |
| `Steam API periodic check interval (seconds)` | float | `1800` | How often to re-check online players |
| `Steam API retry interval on failure (seconds)` | float | `1800` | Retry delay on API error |
| `Over-limit player: delay before kick after warning (seconds)` | float | `300` | Seconds between warning and kick |
| `Private profile: delay before kick after warning (seconds)` | float | `300` | Seconds between private-profile warning and kick |
| `Private profile: max warning kicks before BAN` | int | `2` | Kick count that triggers a BAN |
| `BAN duration (seconds)` | float | `86400` | BAN length (default: 24 hours) |
| `Skip checks for Oxide admins` | bool | `true` | Exempt server admins automatically |
| `Enable debug logging` | bool | `false` | Verbose logging to server console |

---

## Permissions

| Permission | Description |
|------------|-------------|
| `beginguard.exempt` | Skip all checks (use for VIPs, admins, etc.) |
| `beginguard.admin` | Use `bg.*` console commands from in-game console |

```
# Grant to a group
oxide.grant group <group> beginguard.exempt
oxide.grant group <group> beginguard.admin

# Grant to a specific player
oxide.grant user <SteamID64> beginguard.exempt
```

---

## Console Commands

All commands work from the **server console** and **RCON** without extra permissions.  
In-game console requires the `beginguard.admin` permission.

| Command | Description |
|---------|-------------|
| `bg.help` | Show command list |
| `bg.check <SteamID64>` | Show stored record for a player |
| `bg.unban <SteamID64>` | Lift an active BAN |
| `bg.forcecheck <SteamID64>` | Force an immediate Steam API check (player must be online) |
| `bg.reset <SteamID64>` | Reset all stored data for a player |
| `bg.debug <on\|off>` | Toggle debug logging at runtime |

---

## Localization

Language files are stored in `oxide/lang/{language}/BeginnerGuard.json`.  
They are **auto-generated** when the plugin loads for the first time.

| Language | Code | Status |
|----------|------|--------|
| English | `en` | Built-in (default) |
| Japanese | `ja` | Built-in |

See [`lang/en/BeginnerGuard.json`](lang/en/BeginnerGuard.json) for an annotated example.

### Adding a new language

1. Copy `oxide/lang/en/BeginnerGuard.json` to `oxide/lang/<code>/BeginnerGuard.json`  
   (e.g. `oxide/lang/zh/BeginnerGuard.json` for Chinese)
2. Translate the values — **do not change the keys**
3. Run `oxide.reload BeginnerGuard`

### Message placeholders

| Key | `{0}` | `{1}` | `{2}` | `{3}` |
|-----|-------|-------|-------|-------|
| `PrivateProfile.GraceWarn` | remaining minutes | — | — | — |
| `PrivateProfile.WarnKick` | kick delay (s) | current warning count | max warnings | ban hours |
| `PrivateProfile.BanKickReason` | ban hours | — | — | — |
| `PrivateProfile.BanConnectKick` | hours remaining | minutes remaining | — | — |
| `OverLimit.Warn` | player hours | hour limit | kick delay (s) | — |
| `OverLimit.KickReason` | player hours | hour limit | — | — |

---

## How It Works

```
Player connects
    │
    ├─ Is exempt (admin / beginguard.exempt)? → Allow
    │
    ├─ Is currently BAN'd? → Kick with time remaining
    │
    └─ Fetch Steam API
           │
           ├─ Private profile / API error
           │       ├─ Within grace period? → Warn in chat, schedule kick at grace expiry
           │       ├─ Over grace, warnings < max? → Warning kick (increments counter)
           │       └─ Over grace, warnings ≥ max? → BAN issued
           │
           └─ Public profile
                   ├─ Hours ≤ limit? → Allow
                   └─ Hours > limit? → Warn in chat, kick after delay
```

---

## Data Storage

Player records are stored in `oxide/data/BeginnerGuard.json` and persist across server restarts.  
Each record tracks: Steam hours, profile visibility, cumulative server playtime, kick count, BAN expiry.

---

## License

[GPL v3](LICENSE) — Copyright (C) 2024 Mazurk4_

You are free to use, modify, and redistribute this plugin, but any distributed
modified version must also be published under GPL v3 with the original copyright notice intact.
See [LICENSE](LICENSE) for details.
