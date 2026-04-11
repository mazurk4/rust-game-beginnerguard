## Overview

**BeginnerGuard** protects beginner-only Rust servers by automatically checking each player's Steam Rust playtime via the Steam Web API. Players who exceed the configured hour limit — or who hide their playtime behind a private Steam profile — are warned in chat and kicked (or banned after repeated violations).

---

## Features

- **Playtime check on connect** — fetches Steam Rust hours for every joining player
- **Over-limit kick** — configurable playtime cap; warns in chat then kicks after a delay
- **Private profile protection** — grace period allows short visits, then escalates to warning kicks and finally a time-limited BAN
- **Automatic BAN expiry** — bans lift themselves; no admin action needed
- **Exempt permission** — whitelist VIPs, staff, or trusted players with `beginguard.exempt`
- **Periodic re-check** — re-validates all online players at a configurable interval
- **Colored chat warnings** — in-game messages rendered in orange for visibility
- **Multi-language support** — English and Japanese built-in; add any language via `oxide/lang/`

---

## Requirements

- A free **Steam Web API key**: https://steamcommunity.com/dev/apikey

---

## Installation

1. Upload `BeginnerGuard.cs` to `oxide/plugins/`
2. Restart the server or run `oxide.reload BeginnerGuard`
3. Set your Steam API key in `oxide/config/BeginnerGuard.json`
4. Reload: `oxide.reload BeginnerGuard`

---

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Steam API Key | *(required)* | Your Steam Web API key |
| Max allowed Rust playtime (hours) | `1000` | Players above this cap are kicked |
| Private profile grace period (minutes) | `120` | Cumulative server time allowed before kick |
| Periodic check interval (seconds) | `1800` | How often to re-check online players |
| Over-limit kick delay (seconds) | `300` | Time between warning and kick for over-limit players |
| Private profile kick delay (seconds) | `300` | Time between warning and kick for private profiles |
| Warning kicks before BAN | `2` | How many warning kicks before a BAN is issued |
| BAN duration (seconds) | `86400` | BAN length (default: 24 h) |
| Skip checks for Oxide admins | `true` | Auto-exempt server admins |
| Enable debug logging | `false` | Verbose server console output |

---

## Permissions

| Permission | Effect |
|------------|--------|
| `beginguard.exempt` | Bypass all checks |
| `beginguard.admin` | Use `bg.*` commands from in-game console |

---

## Commands

| Command | Description |
|---------|-------------|
| `bg.help` | List all commands |
| `bg.check <SteamID64>` | View a player's stored record |
| `bg.unban <SteamID64>` | Lift an active BAN |
| `bg.forcecheck <SteamID64>` | Force an immediate Steam API check |
| `bg.reset <SteamID64>` | Clear all stored data for a player |
| `bg.debug <on\|off>` | Toggle debug logging at runtime |

---

## Localization

Language files live in `oxide/lang/{code}/BeginnerGuard.json` and are generated automatically on first load.

**Built-in languages:** English (`en`), Japanese (`ja`)

To add a new language, copy `oxide/lang/en/BeginnerGuard.json` to `oxide/lang/<code>/BeginnerGuard.json`, translate the values, and reload the plugin.

---

## License

GPL v3 — Copyright (C) 2024 Mazurk4_

You may use, modify, and redistribute this plugin. Any distributed modified version
must also be published under GPL v3 with the original copyright notice intact.
