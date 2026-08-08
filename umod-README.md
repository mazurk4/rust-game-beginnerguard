## Overview

**Beginner Guard** automatically keeps over-experienced players off your beginner server.

On connect, each player's Rust playtime is fetched from the Steam Web API. Players over the configured hour limit are warned in chat and kicked after a delay. Players whose game details or playtime cannot be read are handled separately: they receive a grace period, then escalating warning kicks, and finally a temporary BAN if they keep reconnecting without fixing their privacy settings.

Everything is configurable — playtime cap, grace period, kick delays, BAN duration — with no hard-coded values.

---

## How Players Are Handled

- **Hours ≤ limit, playtime available** — Allowed in
- **Hours > limit** — Chat warning → kicked after delay
- **Game details/playtime unavailable, within grace period** — Chat warning + kick scheduled at grace expiry
- **Playtime unavailable, over grace (warnings remaining)** — Warning kick
- **Playtime unavailable, warnings exhausted** — Temporary BAN
- **Reconnecting while BAN'd** — Instant kick showing time remaining

---

## Features

- Playtime gate with configurable hour cap
- Unavailable playtime: grace period → warning kicks → time-limited BAN
- Automatic BAN expiry — no manual cleanup needed
- `beginnerguard.exempt` permission to whitelist VIPs and trusted players
- Periodic re-check of all online players
- `/bgstatus` player self-check using the latest cached Steam decision without an additional API request
- Orange colored chat warnings (`#FFA500`) for easy visibility
- Multi-language — English and Japanese built-in; add more via `oxide/lang/`
- Discord Webhook notifications with independent switches for grace, kick, BAN, reconnect, expiry, and manual unban events
- Optional post-BAN visibility recheck with escalation to a longer repeating BAN

---

## Requirements

A free **Steam Web API key** is required: https://steamcommunity.com/dev/apikey

Players must make Steam **Game details** public and make total playtime visible. A zero playtime value is treated as unavailable because Steam can return `0` when total playtime is hidden; genuinely new players receive the normal grace period. Steam API failures are handled fail-open and retried later to avoid false kicks.

---

## Installation

1. Upload `BeginnerGuard.cs` to `oxide/plugins/`
2. Run `oxide.reload BeginnerGuard`
3. Open `oxide/config/BeginnerGuard.json` — set `"Steam API Key"` to your key
4. Run `oxide.reload BeginnerGuard` again

---

## Configuration

- **Steam API Key** *(required)* — Your Steam Web API key
- **Max allowed Rust playtime (hours)** — default `1000` — Players above this are kicked
- **Private profile grace period (minutes)** — default `120` — Total server time allowed when Steam playtime is unavailable
- **Periodic check interval (seconds)** — default `1800` — How often online players are re-checked (30 min)
- **API retry interval on failure (seconds)** — default `1800` — Retry delay when Steam API is unreachable
- **Over-limit kick delay (seconds)** — default `300` — Delay between chat warning and kick
- **Private profile kick delay (seconds)** — default `300` — Delay between chat warning and kick
- **Warning kicks before BAN** — default `2` — How many warning kicks before a BAN is issued
- **BAN duration (seconds)** — default `86400` — BAN length (default: 24 hours)
- **Private profile BAN grace** — disabled by default — Recheck visibility after the initial BAN; if still unavailable, apply the configured escalated BAN duration
- **Skip checks for Oxide admins** — default `true` — Auto-exempt server admins
- **Enable debug logging** — default `false` — Verbose output to server console
- **Discord webhook notifications** — default all `false` — Set a Webhook URL and enable only the enforcement stages you want reported

---

## Permissions

- `beginnerguard.exempt` — Skip all checks — for VIPs and trusted regulars
- `beginnerguard.admin` — Use `bg.*` commands from the in-game F1 console
- `beginnerguard.status` — Use `/bgstatus` and `/bgstatus steam` in chat

```
oxide.grant group  <group>      beginnerguard.exempt
oxide.grant group  <group>      beginnerguard.admin
oxide.grant user   <SteamID64>  beginnerguard.exempt
oxide.grant group  default      beginnerguard.status
oxide.grant user   <SteamID64>  beginnerguard.status
```

Grant the permission to the `default` group for all players, or grant it only to selected SteamID64 users. Use the corresponding `oxide.revoke` command to remove access.

---

## Commands

Players with `beginnerguard.status` can use `/bgstatus` in chat to see their own latest cached Steam visibility and playtime decision. When visibility cannot be verified, the result directs them to `/bgstatus steam`, which displays the Steam privacy setup instructions. Neither command makes a Steam API request.

### Administration commands

All commands work from the **server console / RCON** without permissions.  
Requires `beginnerguard.admin` when used from the **in-game F1 console**.

- `bg.help` — Show command list
- `bg.check <SteamID64>` — View a player's stored record
- `bg.unban <SteamID64>` — Lift an active BAN
- `bg.forcecheck <SteamID64>` — Force an immediate Steam API check (player must be online)
- `bg.reset <SteamID64>` — Clear all stored data for a player
- `bg.debug <on|off>` — Toggle debug logging without a reload

Temporary BANs are enforced by this plugin on reconnect; they are not added to Rust's native ban list.

`bg.unban` clears the BAN expiry, BAN stage, and warning-kick count, but keeps cumulative server playtime. The player is checked normally on their next connection; use `bg.forcecheck` for an online player or `bg.reset` to clear all stored state.

---

## Localization

Language files are stored in `oxide/lang/{code}/BeginnerGuard.json` and auto-generated on first load.

**Built-in:** English (`en`), Japanese (`ja`)

To add a new language, copy `oxide/lang/en/BeginnerGuard.json` to `oxide/lang/<code>/BeginnerGuard.json`, translate the values (do not change the keys), and reload.

---

## Source & Contributing

Source code is available on GitHub: https://github.com/mazurk4/rust-game-beginnerguard

Bug reports, feature requests, and pull requests are welcome — feel free to open an issue or submit a PR.
