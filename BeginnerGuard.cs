using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Beginner Guard", "Mazurk4_", "1.4.0")]
    [Description("Beginner server protection - restricts players by Rust Steam playtime")]
    public class BeginnerGuard : RustPlugin
    {
        // ---------------------------------------------------------------
        // Permissions
        //
        //  beginnerguard.exempt  - Skip all checks (whitelist equivalent)
        //  beginnerguard.admin   - Use management console commands
        //
        // Grant to a group:  oxide.grant group <group> beginnerguard.exempt
        // Grant to a user:   oxide.grant user  <sid>   beginnerguard.admin
        // Revoke:            oxide.revoke group <group> beginnerguard.exempt
        // ---------------------------------------------------------------
        private const string PermExempt = "beginnerguard.exempt";
        private const string PermAdmin  = "beginnerguard.admin";

        // ---------------------------------------------------------------
        // Configuration
        // ---------------------------------------------------------------
        private PluginConfig _config;

        private class PluginConfig
        {
            [JsonProperty("Steam API Key")]
            public string SteamApiKey { get; set; } = "YOUR_STEAM_API_KEY_HERE";

            [JsonProperty("Max allowed Rust playtime on Steam (hours)")]
            public int MaxSteamHours { get; set; } = 1000;

            [JsonProperty("Private profile: max cumulative server playtime before kick (minutes)")]
            public int PrivateProfileMaxMinutes { get; set; } = 120;

            [JsonProperty("Steam API periodic check interval (seconds)")]
            public float CheckIntervalSeconds { get; set; } = 1800f;

            [JsonProperty("Steam API retry interval on failure (seconds)")]
            public float ApiRetryIntervalSeconds { get; set; } = 1800f;

            [JsonProperty("Over-limit player: delay before kick after warning (seconds)")]
            public float OverLimitKickDelaySeconds { get; set; } = 300f;

            [JsonProperty("Private profile: delay before kick after warning (seconds)")]
            public float PrivateProfileKickDelaySeconds { get; set; } = 300f;

            [JsonProperty("Private profile: max warning kicks before BAN")]
            public int KickCountBeforeBan { get; set; } = 2;

            [JsonProperty("BAN duration (seconds)")]
            public float BanDurationSeconds { get; set; } = 86400f;

            [JsonProperty("Skip checks for Oxide admins")]
            public bool SkipAdmins { get; set; } = true;

            [JsonProperty("Enable debug logging")]
            public bool DebugLogging { get; set; } = false;
        }

        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintError("Failed to load config.json — using defaults.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        // ---------------------------------------------------------------
        // Localization  →  oxide/lang/{lang}/BeginnerGuard.json
        // ---------------------------------------------------------------
        protected override void LoadDefaultMessages()
        {
            // English (default)
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PrivateProfile.GraceWarn"]       = "<color=#FFA500>[BeginnerGuard] Your Steam profile is set to private.\nYou will be kicked in approximately {0} minute(s) if it remains private.\nHow to fix: Steam → Profile → Edit Profile → Privacy Settings → set to Public.</color>",
                ["PrivateProfile.GraceKickReason"] = "[BeginnerGuard] Kicked: cumulative server playtime limit reached.\nPlease set your Steam profile to public and reconnect.",
                ["PrivateProfile.WarnKick"]        = "<color=#FFA500>[BeginnerGuard] Your Steam profile is private!\nPlease set it to public within {0}s.\nWarning {1}/{2} — a {3}h BAN will be issued if you exceed this.\nHow to fix: Steam → Profile → Edit Profile → Privacy Settings → Public.</color>",
                ["PrivateProfile.WarnKickReason"]  = "[BeginnerGuard] Kicked: Steam profile is private.\nPlease set your profile to public and reconnect.",
                ["PrivateProfile.BanKickReason"]   = "[BeginnerGuard] You have been banned for {0} hour(s).\nReason: Steam profile remained private after repeated warnings.\nPlease set your Steam profile to public before reconnecting.\nHow to fix: Steam → Profile → Edit Profile → Privacy Settings → Public.",
                ["PrivateProfile.BanConnectKick"]  = "[BeginnerGuard] You are banned. Ban expires in: {0}h {1}m\nPlease set your Steam profile to public before reconnecting.",
                ["OverLimit.Warn"]                 = "<color=#FFA500>[BeginnerGuard] This is a beginner-only server.\nYour Rust playtime on Steam: {0}h (limit: {1}h).\nYou will be kicked in {2}s. Please find a server that matches your experience level.</color>",
                ["OverLimit.KickReason"]           = "[BeginnerGuard] Kicked: playtime too high ({0}h / limit {1}h).\nThis server is for beginners only. Thanks for understanding!",
            }, this);

            // Japanese
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PrivateProfile.GraceWarn"]       = "<color=#FFA500>[BeginnerGuard] あなたのSteamプロフィールは非公開に設定されています。\nこのまま非公開の場合、約{0}分後にキックされます。\n修正方法: Steam → プロフィール → プロフィールを編集 → プライバシー設定 → 公開に変更</color>",
                ["PrivateProfile.GraceKickReason"] = "[BeginnerGuard] キック: サーバー滞在時間の上限に達しました。\nSteamプロフィールを公開に設定して再接続してください。",
                ["PrivateProfile.WarnKick"]        = "<color=#FFA500>[BeginnerGuard] あなたのSteamプロフィールが非公開です！\n{0}秒以内に公開に設定してください。\n警告 {1}/{2} — 超過した場合は{3}時間のBANが適用されます。\n修正方法: Steam → プロフィール → プロフィールを編集 → プライバシー設定 → 公開</color>",
                ["PrivateProfile.WarnKickReason"]  = "[BeginnerGuard] キック: Steamプロフィールが非公開です。\nプロフィールを公開に設定して再接続してください。",
                ["PrivateProfile.BanKickReason"]   = "[BeginnerGuard] {0}時間のBANが適用されました。\n理由: 警告後もSteamプロフィールが非公開のままでした。\n再接続前にSteamプロフィールを公開に設定してください。\n修正方法: Steam → プロフィール → プロフィールを編集 → プライバシー設定 → 公開",
                ["PrivateProfile.BanConnectKick"]  = "[BeginnerGuard] BANされています。解除まで: {0}時間{1}分\n再接続前にSteamプロフィールを公開に設定してください。",
                ["OverLimit.Warn"]                 = "<color=#FFA500>[BeginnerGuard] このサーバーは初心者専用です。\nあなたのRust Steamプレイ時間: {0}時間（上限: {1}時間）\n{2}秒後にキックされます。ご自身の経験に合ったサーバーをお探しください。</color>",
                ["OverLimit.KickReason"]           = "[BeginnerGuard] キック: プレイ時間が超過しています（{0}時間 / 上限 {1}時間）\nこのサーバーは初心者専用です。ご理解ありがとうございます！",
            }, this, "ja");
        }

        // ---------------------------------------------------------------
        // Data  →  oxide/data/BeginnerGuard.json
        //
        // Root wrapper class is required so Oxide serialises as an object
        // {\"Players\":{...}} rather than a bare dictionary, which can
        // deserialise back to null on some Oxide versions.
        // ---------------------------------------------------------------
        private StoredData _data = new StoredData();
        private const string DataFileName = "BeginnerGuard";

        private class PlayerRecord
        {
            public string SteamId               { get; set; } = string.Empty;
            public string DisplayName           { get; set; } = string.Empty;
            public int    SteamTotalHours       { get; set; } = -1;     // -1 = not yet fetched
            public bool   IsProfilePrivate      { get; set; } = false;
            public double ServerPlaytimeMinutes { get; set; } = 0.0;    // cumulative on this server
            // LastJoinTime stored as UTC ticks (long) to avoid DateTime? JSON issues
            public long   LastJoinTicks         { get; set; } = 0;      // 0 = not connected
            public int    PrivateKickCount      { get; set; } = 0;
            // BannedUntil stored as UTC ticks; 0 = not banned
            public long   BannedUntilTicks      { get; set; } = 0;
            // LastSteamCheck stored as UTC ticks; 0 = never checked
            public long   LastSteamCheckTicks   { get; set; } = 0;

            // ---- helpers (not serialised) ----
            [JsonIgnore] public DateTime? LastJoinTime
            {
                get => LastJoinTicks > 0 ? new DateTime(LastJoinTicks, DateTimeKind.Utc) : (DateTime?)null;
                set => LastJoinTicks = value.HasValue ? value.Value.Ticks : 0;
            }
            [JsonIgnore] public DateTime? BannedUntil
            {
                get => BannedUntilTicks > 0 ? new DateTime(BannedUntilTicks, DateTimeKind.Utc) : (DateTime?)null;
                set => BannedUntilTicks = value.HasValue ? value.Value.Ticks : 0;
            }
            [JsonIgnore] public DateTime LastSteamCheck
            {
                get => LastSteamCheckTicks > 0 ? new DateTime(LastSteamCheckTicks, DateTimeKind.Utc) : DateTime.MinValue;
                set => LastSteamCheckTicks = value.Ticks;
            }
        }

        private class StoredData
        {
            public Dictionary<string, PlayerRecord> Players { get; set; }
                = new Dictionary<string, PlayerRecord>();
        }

        private void LoadData()
        {
            try
            {
                var loaded = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFileName);
                _data = loaded ?? new StoredData();
                if (_data.Players == null) _data.Players = new Dictionary<string, PlayerRecord>();
                DebugLog($"Data loaded — {_data.Players.Count} player record(s).");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load data file: {ex.Message} — starting fresh.");
                _data = new StoredData();
            }
        }

        private void SaveData()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject(DataFileName, _data);
                DebugLog($"Data saved — {_data.Players.Count} player record(s).");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to save data file: {ex.Message}");
            }
        }

        private PlayerRecord GetOrCreateRecord(BasePlayer player)
        {
            var sid = player.UserIDString;
            if (!_data.Players.TryGetValue(sid, out var record))
            {
                record = new PlayerRecord { SteamId = sid };
                _data.Players[sid] = record;
                DebugLog($"Created new record for {player.displayName} ({sid}).");
            }
            record.DisplayName = player.displayName;
            return record;
        }

        // ---------------------------------------------------------------
        // Timers
        // ---------------------------------------------------------------
        private Timer _periodicCheckTimer;
        private readonly Dictionary<string, Timer> _pendingKickTimers
            = new Dictionary<string, Timer>();

        // ---------------------------------------------------------------
        // Oxide Hooks
        // ---------------------------------------------------------------
        private void Init()
        {
            permission.RegisterPermission(PermExempt, this);
            permission.RegisterPermission(PermAdmin,  this);
            LoadData();

            Puts("BeginnerGuard initialised.");
            Puts($"  Exempt permission : {PermExempt}");
            Puts($"  Admin permission  : {PermAdmin}");
            Puts($"  Debug logging     : {(_config.DebugLogging ? "ON" : "OFF")}");
        }

        private void OnServerInitialized()
        {
            _periodicCheckTimer = timer.Every(_config.CheckIntervalSeconds, () =>
            {
                DebugLog("Periodic Steam check triggered.");
                foreach (var player in BasePlayer.activePlayerList)
                    FetchAndProcessSteamHours(player);
            });
            Puts($"Periodic check scheduled every {_config.CheckIntervalSeconds}s.");
        }

        private void Unload()
        {
            _periodicCheckTimer?.Destroy();
            foreach (var t in _pendingKickTimers.Values) t?.Destroy();
            SaveData();
            Puts("BeginnerGuard unloaded.");
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (IsExempt(player))
            {
                DebugLog($"{player.displayName} ({player.UserIDString}) is exempt — skipping.");
                return;
            }

            var record = GetOrCreateRecord(player);

            // BAN check
            if (record.BannedUntil.HasValue)
            {
                if (DateTime.UtcNow < record.BannedUntil.Value)
                {
                    var rem = record.BannedUntil.Value - DateTime.UtcNow;
                    DebugLog($"{player.displayName} is BAN'd for another {rem.TotalMinutes:F0} min.");
                    KickPlayer(player,
                        GetMsg("PrivateProfile.BanConnectKick", player, rem.Hours, rem.Minutes));
                    return;
                }
                // Expired — auto-lift
                DebugLog($"BAN expired for {player.displayName} — lifting automatically.");
                record.BannedUntil      = null;
                record.PrivateKickCount = 0;
            }

            record.LastJoinTime = DateTime.UtcNow;
            SaveData();

            DebugLog($"{player.displayName} connected — starting Steam check.");
            FetchAndProcessSteamHours(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            CancelPendingKick(player.UserIDString);

            if (!_data.Players.TryGetValue(player.UserIDString, out var record)) return;
            if (!record.LastJoinTime.HasValue) return;

            double session = (DateTime.UtcNow - record.LastJoinTime.Value).TotalMinutes;
            record.ServerPlaytimeMinutes += session;
            record.LastJoinTime           = null;

            DebugLog($"{player.displayName} disconnected — session {session:F1} min, " +
                     $"cumulative {record.ServerPlaytimeMinutes:F1} min.");
            SaveData();
        }

        // ---------------------------------------------------------------
        // Steam API
        // ---------------------------------------------------------------
        private void FetchAndProcessSteamHours(BasePlayer player)
        {
            if (IsExempt(player)) return;

            var sid = player.UserIDString;
            var url = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/" +
                      $"?key={_config.SteamApiKey}&steamid={sid}" +
                      "&include_appinfo=false&appids_filter[0]=252490&format=json";

            DebugLog($"Fetching Steam hours for {player.displayName} ({sid})...");

            webrequest.Enqueue(url, null, (code, response) =>
            {
                if (!player.IsConnected)
                {
                    DebugLog($"{player.displayName} disconnected before API response — ignoring.");
                    return;
                }

                var record = GetOrCreateRecord(player);

                // --- API failure ---
                if (code != 200 || string.IsNullOrEmpty(response))
                {
                    PrintWarning($"[BeginnerGuard] Steam API failed (HTTP {code}) for {player.displayName}. " +
                                 $"Retrying in {_config.ApiRetryIntervalSeconds}s.");
                    timer.Once(_config.ApiRetryIntervalSeconds, () =>
                    {
                        if (player.IsConnected) FetchAndProcessSteamHours(player);
                    });
                    return;
                }

                DebugLog($"Steam API response received for {player.displayName} (HTTP {code}).");

                try
                {
                    var root    = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                    var respObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                      root["response"].ToString());

                    // game_count missing or 0 → private profile / Rust not owned
                    if (!respObj.ContainsKey("game_count") || respObj["game_count"].ToString() == "0")
                    {
                        DebugLog($"{player.displayName}: game_count=0 or missing → private/not owned.");
                        HandlePrivateProfile(player, record);
                        return;
                    }

                    var games = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                                    respObj["games"].ToString());

                    if (games == null || games.Count == 0)
                    {
                        DebugLog($"{player.displayName}: games list empty → private/not owned.");
                        HandlePrivateProfile(player, record);
                        return;
                    }

                    int minutesPlayed = 0;
                    foreach (var g in games)
                        if (g.ContainsKey("playtime_forever"))
                            minutesPlayed = Convert.ToInt32(g["playtime_forever"]);

                    int hours = minutesPlayed / 60;
                    record.SteamTotalHours  = hours;
                    record.IsProfilePrivate = false;
                    record.LastSteamCheck   = DateTime.UtcNow;
                    SaveData();

                    Puts($"[BeginnerGuard] {player.displayName} — Steam Rust hours: {hours}h " +
                         $"(limit: {_config.MaxSteamHours}h)");

                    if (hours > _config.MaxSteamHours)
                        HandleOverLimitPlayer(player, record, hours);
                    else
                        DebugLog($"{player.displayName} is within the hour limit — allowed.");
                }
                catch (Exception ex)
                {
                    PrintError($"[BeginnerGuard] Failed to parse Steam API response for " +
                               $"{player.displayName}: {ex.Message}");
                }

            }, this);
        }

        // ---------------------------------------------------------------
        // Enforcement
        // ---------------------------------------------------------------
        private void HandlePrivateProfile(BasePlayer player, PlayerRecord record)
        {
            record.IsProfilePrivate = true;
            record.LastSteamCheck   = DateTime.UtcNow;
            SaveData();

            double currentSession = record.LastJoinTime.HasValue
                ? (DateTime.UtcNow - record.LastJoinTime.Value).TotalMinutes : 0.0;
            double totalMinutes   = record.ServerPlaytimeMinutes + currentSession;

            DebugLog($"{player.displayName}: private profile. " +
                     $"Cumulative server time = {totalMinutes:F1} min " +
                     $"(limit: {_config.PrivateProfileMaxMinutes} min). " +
                     $"Kick count = {record.PrivateKickCount}/{_config.KickCountBeforeBan}.");

            if (totalMinutes < _config.PrivateProfileMaxMinutes)
            {
                // Still within grace period — warn and schedule kick at time-limit
                double remaining = _config.PrivateProfileMaxMinutes - totalMinutes;
                SendMsg(player, GetMsg("PrivateProfile.GraceWarn", player, remaining.ToString("F0")));

                ScheduleKick(player, (float)(remaining * 60f),
                    GetMsg("PrivateProfile.GraceKickReason", player));
                return;
            }

            // Over the cumulative limit
            if (record.PrivateKickCount >= _config.KickCountBeforeBan)
            {
                // Issue BAN
                record.BannedUntil      = DateTime.UtcNow.AddSeconds(_config.BanDurationSeconds);
                record.PrivateKickCount = 0;
                SaveData();

                double banHours = _config.BanDurationSeconds / 3600.0;
                Puts($"[BeginnerGuard] BAN issued to {player.displayName} ({player.UserIDString}) " +
                     $"for {banHours:F0}h — private profile.");
                KickPlayer(player,
                    GetMsg("PrivateProfile.BanKickReason", player, banHours.ToString("F0")));
            }
            else
            {
                // Warning kick
                record.PrivateKickCount++;
                SaveData();

                SendMsg(player, GetMsg("PrivateProfile.WarnKick", player,
                    _config.PrivateProfileKickDelaySeconds.ToString("F0"),
                    record.PrivateKickCount,
                    _config.KickCountBeforeBan,
                    (_config.BanDurationSeconds / 3600).ToString("F0")));

                ScheduleKick(player, _config.PrivateProfileKickDelaySeconds,
                    GetMsg("PrivateProfile.WarnKickReason", player));
            }
        }

        private void HandleOverLimitPlayer(BasePlayer player, PlayerRecord record, int hours)
        {
            SendMsg(player, GetMsg("OverLimit.Warn", player,
                hours, _config.MaxSteamHours, _config.OverLimitKickDelaySeconds.ToString("F0")));

            ScheduleKick(player, _config.OverLimitKickDelaySeconds,
                GetMsg("OverLimit.KickReason", player, hours, _config.MaxSteamHours));
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private bool IsExempt(BasePlayer player)
        {
            if (_config.SkipAdmins && player.IsAdmin) return true;
            if (permission.UserHasPermission(player.UserIDString, PermExempt)) return true;
            return false;
        }

        private void ScheduleKick(BasePlayer player, float delaySec, string reason)
        {
            var sid = player.UserIDString;
            CancelPendingKick(sid);
            DebugLog($"Kick scheduled for {player.displayName} in {delaySec}s.");
            _pendingKickTimers[sid] = timer.Once(delaySec, () =>
            {
                _pendingKickTimers.Remove(sid);
                if (player.IsConnected) KickPlayer(player, reason);
            });
        }

        private void CancelPendingKick(string steamId)
        {
            if (_pendingKickTimers.TryGetValue(steamId, out var t))
            {
                t?.Destroy();
                _pendingKickTimers.Remove(steamId);
                DebugLog($"Pending kick cancelled for {steamId}.");
            }
        }

        private void KickPlayer(BasePlayer player, string reason)
        {
            if (!player.IsConnected) return;
            Puts($"[BeginnerGuard] KICK {player.displayName} ({player.UserIDString}) — {reason.Replace("\n", " | ")}");
            player.Kick(reason);
        }

        private void SendMsg(BasePlayer player, string msg)
        {
            if (player.IsConnected) player.ChatMessage(msg);
        }

        private string GetMsg(string key, BasePlayer player, params object[] args)
        {
            string msg = lang.GetMessage(key, this, player?.UserIDString);
            return args.Length > 0 ? string.Format(msg, args) : msg;
        }

        private void DebugLog(string msg)
        {
            if (_config?.DebugLogging == true)
                Puts($"[DEBUG] {msg}");
        }

        // ---------------------------------------------------------------
        // Console Commands  (server console / RCON = always allowed)
        //                   (in-game console   = requires beginnerguard.admin)
        //
        // Grant:   oxide.grant group moderator beginnerguard.admin
        // Revoke:  oxide.revoke group moderator beginnerguard.admin
        // ---------------------------------------------------------------

        [ConsoleCommand("bg.help")]
        private void CmdHelp(ConsoleSystem.Arg arg)
        {
            if (!HasAdminPerm(arg)) return;
            arg.ReplyWith(
                "=== BeginnerGuard Commands ===\n" +
                "bg.check      <SteamID64>  Show player record\n" +
                "bg.unban      <SteamID64>  Lift an active BAN\n" +
                "bg.forcecheck <SteamID64>  Force an immediate Steam API check (player must be online)\n" +
                "bg.reset      <SteamID64>  Reset all stored data for a player\n" +
                "bg.debug      <on|off>     Toggle debug logging at runtime\n" +
                "bg.help                    Show this help\n" +
                "\n" +
                "=== Permission Management ===\n" +
                $"  oxide.grant  group <group> {PermAdmin}   -- grant admin commands\n" +
                $"  oxide.grant  group <group> {PermExempt}  -- grant check exemption\n" +
                $"  oxide.grant  user  <sid>   {PermAdmin}   -- per-user grant\n" +
                $"  oxide.revoke group <group> {PermAdmin}   -- revoke");
        }

        [ConsoleCommand("bg.check")]
        private void CmdCheck(ConsoleSystem.Arg arg)
        {
            if (!HasAdminPerm(arg)) return;
            var sid = arg.GetString(0);
            if (string.IsNullOrEmpty(sid)) { arg.ReplyWith("Usage: bg.check <SteamID64>"); return; }

            if (_data.Players.TryGetValue(sid, out var r))
            {
                string banStr  = r.BannedUntil.HasValue
                    ? r.BannedUntil.Value.ToString("u") : "none";
                string checkStr = r.LastSteamCheck != DateTime.MinValue
                    ? r.LastSteamCheck.ToString("u") : "never";

                arg.ReplyWith(
                    $"=== {r.DisplayName} ({r.SteamId}) ===\n" +
                    $"Steam Rust hours    : {r.SteamTotalHours}h\n" +
                    $"Profile private     : {r.IsProfilePrivate}\n" +
                    $"Server playtime     : {r.ServerPlaytimeMinutes:F1} min\n" +
                    $"Kick count          : {r.PrivateKickCount} / {_config.KickCountBeforeBan}\n" +
                    $"Banned until (UTC)  : {banStr}\n" +
                    $"Last Steam check    : {checkStr}");
            }
            else
            {
                arg.ReplyWith($"No record found for {sid}.");
            }
        }

        [ConsoleCommand("bg.unban")]
        private void CmdUnban(ConsoleSystem.Arg arg)
        {
            if (!HasAdminPerm(arg)) return;
            var sid = arg.GetString(0);
            if (string.IsNullOrEmpty(sid)) { arg.ReplyWith("Usage: bg.unban <SteamID64>"); return; }

            if (_data.Players.TryGetValue(sid, out var r))
            {
                r.BannedUntil      = null;
                r.PrivateKickCount = 0;
                SaveData();
                arg.ReplyWith($"[BeginnerGuard] BAN lifted for {r.DisplayName} ({sid}).");
                Puts($"[BeginnerGuard] BAN manually lifted for {r.DisplayName} ({sid}).");
            }
            else
            {
                arg.ReplyWith($"No record found for {sid}.");
            }
        }

        [ConsoleCommand("bg.forcecheck")]
        private void CmdForceCheck(ConsoleSystem.Arg arg)
        {
            if (!HasAdminPerm(arg)) return;
            var sid = arg.GetString(0);
            if (string.IsNullOrEmpty(sid)) { arg.ReplyWith("Usage: bg.forcecheck <SteamID64>"); return; }

            ulong uid;
            if (!ulong.TryParse(sid, out uid))
            {
                arg.ReplyWith("Invalid SteamID64.");
                return;
            }

            var player = BasePlayer.FindByID(uid);
            if (player == null || !player.IsConnected)
            {
                arg.ReplyWith($"{sid} is not currently online.");
                return;
            }
            FetchAndProcessSteamHours(player);
            arg.ReplyWith($"[BeginnerGuard] Forced Steam check started for {player.displayName}.");
        }

        [ConsoleCommand("bg.reset")]
        private void CmdReset(ConsoleSystem.Arg arg)
        {
            if (!HasAdminPerm(arg)) return;
            var sid = arg.GetString(0);
            if (string.IsNullOrEmpty(sid)) { arg.ReplyWith("Usage: bg.reset <SteamID64>"); return; }

            if (_data.Players.TryGetValue(sid, out var r))
            {
                r.ServerPlaytimeMinutes = 0;
                r.PrivateKickCount      = 0;
                r.BannedUntil           = null;
                r.LastJoinTime          = null;
                r.IsProfilePrivate      = false;
                r.SteamTotalHours       = -1;
                r.LastSteamCheck        = DateTime.MinValue;
                SaveData();
                arg.ReplyWith($"[BeginnerGuard] Record reset for {r.DisplayName} ({sid}).");
                Puts($"[BeginnerGuard] Record manually reset for {r.DisplayName} ({sid}).");
            }
            else
            {
                arg.ReplyWith($"No record found for {sid}.");
            }
        }

        [ConsoleCommand("bg.debug")]
        private void CmdDebug(ConsoleSystem.Arg arg)
        {
            if (!HasAdminPerm(arg)) return;
            var val = arg.GetString(0).ToLower();
            if (val != "on" && val != "off") { arg.ReplyWith("Usage: bg.debug <on|off>"); return; }

            _config.DebugLogging = (val == "on");
            SaveConfig();
            arg.ReplyWith($"[BeginnerGuard] Debug logging is now {val.ToUpper()}.");
            Puts($"[BeginnerGuard] Debug logging set to {val.ToUpper()} by console command.");
        }

        // ---------------------------------------------------------------
        // Permission Helper
        // ---------------------------------------------------------------
        private bool HasAdminPerm(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return true;   // server console / RCON

            var player = arg.Connection?.player as BasePlayer;
            //var player = arg.Player();
            if (player == null) return true;

            if (permission.UserHasPermission(player.UserIDString, PermAdmin)) return true;

            arg.ReplyWith("[BeginnerGuard] You do not have permission to use this command.");
            return false;
        }
    }
}