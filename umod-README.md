## Overview

**Beginner Guard** automatically keeps over-experienced players off your beginner server.

On connect, each player's Rust playtime is fetched from the Steam Web API. Players over the configured hour limit are warned in chat and kicked after a delay. Players hiding their stats with a private Steam profile are handled separately: they receive a grace period, then escalating warning kicks, and finally a temporary BAN if they keep reconnecting without fixing their privacy settings.

Everything is configurable — playtime cap, grace period, kick delays, BAN duration — with no hard-coded values.

---

## How Players Are Handled

| Situation | What Happens |
|-----------|-------------|
| Hours ≤ limit, public profile | Allowed in |
| Hours > limit | Chat warning → kicked after delay |
| Private profile, within grace period | Chat warning + kick scheduled at grace expiry |
| Private profile, over grace (warnings remaining) | Warning kick |
| Private profile, warnings exhausted | Temporary BAN |
| Reconnecting while BAN'd | Instant kick showing time remaining |

---

## Features

- Playtime gate with configurable hour cap
- Private profile: grace period → warning kicks → time-limited BAN
- Automatic BAN expiry — no manual cleanup needed
- `beginnerguard.exempt` permission to whitelist VIPs and trusted players
- Periodic re-check of all online players
- Orange colored chat warnings (`#FFA500`) for easy visibility
- Multi-language — English and Japanese built-in; add more via `oxide/lang/`

---

## Requirements

A free **Steam Web API key** is required: https://steamcommunity.com/dev/apikey

---

## Installation

1. Upload `BeginnerGuard.cs` to `oxide/plugins/`
2. Run `oxide.reload BeginnerGuard`
3. Open `oxide/config/BeginnerGuard.json` — set `"Steam API Key"` to your key
4. Run `oxide.reload BeginnerGuard` again

---

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Steam API Key | *(required)* | Your Steam Web API key |
| Max allowed Rust playtime (hours) | `1000` | Players above this are kicked |
| Private profile grace period (minutes) | `120` | Total server time allowed for private-profile players |
| Periodic check interval (seconds) | `1800` | How often online players are re-checked (30 min) |
| API retry interval on failure (seconds) | `1800` | Retry delay when Steam API is unreachable |
| Over-limit kick delay (seconds) | `300` | Delay between chat warning and kick |
| Private profile kick delay (seconds) | `300` | Delay between chat warning and kick |
| Warning kicks before BAN | `2` | How many warning kicks before a BAN is issued |
| BAN duration (seconds) | `86400` | BAN length (default: 24 hours) |
| Skip checks for Oxide admins | `true` | Auto-exempt server admins |
| Enable debug logging | `false` | Verbose output to server console |

---

## Permissions

| Permission | Effect |
|------------|--------|
| `beginnerguard.exempt` | Skip all checks — for VIPs and trusted regulars |
| `beginnerguard.admin` | Use `bg.*` commands from the in-game F1 console |

```
oxide.grant group  <group>      beginnerguard.exempt
oxide.grant group  <group>      beginnerguard.admin
oxide.grant user   <SteamID64>  beginnerguard.exempt
```

---

## Commands

All commands work from the **server console / RCON** without permissions.  
Requires `beginnerguard.admin` when used from the **in-game F1 console**.

| Command | Description |
|---------|-------------|
| `bg.help` | Show command list |
| `bg.check <SteamID64>` | View a player's stored record |
| `bg.unban <SteamID64>` | Lift an active BAN |
| `bg.forcecheck <SteamID64>` | Force an immediate Steam API check (player must be online) |
| `bg.reset <SteamID64>` | Clear all stored data for a player |
| `bg.debug <on\|off>` | Toggle debug logging without a reload |

---

## Localization

Language files are stored in `oxide/lang/{code}/BeginnerGuard.json` and auto-generated on first load.

**Built-in:** English (`en`), Japanese (`ja`)

To add a new language, copy `oxide/lang/en/BeginnerGuard.json` to `oxide/lang/<code>/BeginnerGuard.json`, translate the values (do not change the keys), and reload.

---

## Source & Contributing

Source code is available on GitHub: https://github.com/mazurk4/rust-game-beginnerguard

Bug reports, feature requests, and pull requests are welcome — feel free to open an issue or submit a PR.
