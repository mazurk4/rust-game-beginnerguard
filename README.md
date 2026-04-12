# Beginner Guard

An [Oxide/uMod](https://umod.org/) plugin for [Rust](https://store.steampowered.com/app/252490/Rust/) that keeps beginner servers beginner-friendly.  
It checks each player's Steam Rust playtime on connect and removes anyone who has outgrown your server's skill level.

**Version:** 1.4.0 | **Author:** Mazurk4_ | **License:** [MIT](LICENSE)

[日本語版 README はこちら](README-JPN.md)

---

## Screenshots

![Chat warning — private profile](docs/screenshots/chat-warning.png)

*Orange chat warning shown when a player's Steam profile is set to private*

---

## What It Does

When a player joins, the plugin queries the **Steam Web API** for their total Rust playtime:

| Situation | Result |
|-----------|--------|
| Hours ≤ limit, public profile | Allowed in |
| Hours > limit | Chat warning → kicked after delay |
| Private Steam profile, within grace period | Chat warning + kick scheduled at grace expiry |
| Private profile, over grace (warnings remaining) | Warning kick (counter +1) |
| Private profile, over grace (warnings exhausted) | Temporary BAN issued |
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
- **Private profile handling** — grace period → warning kicks → temporary BAN
- **Automatic BAN expiry** — bans lift themselves; no admin action needed
- **Exempt permission** — whitelist VIPs, staff, and trusted players
- **Periodic re-check** — re-validates all online players on a schedule
- **Colored chat warnings** — orange `#FFA500` for easy visibility
- **Multi-language** — English and Japanese built-in; add more via `oxide/lang/`

---

## Configuration

File: `oxide/config/BeginnerGuard.json`  
See [`config/BeginnerGuard.json.example`](config/BeginnerGuard.json.example) for a ready-to-use template.

| Setting | Default | Description |
|---------|---------|-------------|
| `Steam API Key` | *(required)* | Your Steam Web API key |
| `Max allowed Rust playtime on Steam (hours)` | `1000` | Players above this are kicked |
| `Private profile: max cumulative server playtime before kick (minutes)` | `120` | Total server time a private-profile player is allowed before kick |
| `Steam API periodic check interval (seconds)` | `1800` | How often online players are re-checked (default: 30 min) |
| `Steam API retry interval on failure (seconds)` | `1800` | Retry delay when Steam API is unreachable |
| `Over-limit player: delay before kick after warning (seconds)` | `300` | Seconds between chat warning and kick |
| `Private profile: delay before kick after warning (seconds)` | `300` | Seconds between chat warning and kick |
| `Private profile: max warning kicks before BAN` | `2` | Warning kick count before a BAN is issued |
| `BAN duration (seconds)` | `86400` | How long the BAN lasts (default: 24 h) |
| `Skip checks for Oxide admins` | `true` | Automatically exempt server admins |
| `Enable debug logging` | `false` | Print verbose logs to the server console |

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
| `bg.debug <on\|off>` | Toggle debug logging without restarting |

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
           ├─ Private profile or API error
           │       ├─ Within grace period?        → Chat warning + kick scheduled at expiry
           │       ├─ Over grace, warnings left?   → Warning kick (counter +1)
           │       └─ Over grace, warnings used up? → BAN issued
           │
           └─ Public profile
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

**To add a new language:**

1. Copy `oxide/lang/en/BeginnerGuard.json` → `oxide/lang/<code>/BeginnerGuard.json`
2. Translate the values — **do not change the keys**
3. `oxide.reload BeginnerGuard`

See [`lang/en/BeginnerGuard.json`](lang/en/BeginnerGuard.json) for the full message list and placeholder reference.

---

## Data Storage

Records are saved to `oxide/data/BeginnerGuard.json` and persist across server restarts.  
Each record stores: Steam hours · profile visibility · cumulative server playtime · kick count · BAN expiry.

---

## Contributing

Bug reports, feature suggestions, and translation PRs are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).

---

## License

[MIT](LICENSE) — Copyright (C) 2024 Mazurk4_
