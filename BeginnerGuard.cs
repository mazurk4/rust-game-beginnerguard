using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Beginner Guard", "Mazurk4_", "1.6.0")]
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
        private const int RustAppId = 252490;

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

            [JsonProperty("Private profile BAN grace")]
            public BanGraceConfig BanGrace { get; set; } = new BanGraceConfig();

            [JsonProperty("Skip checks for Oxide admins")]
            public bool SkipAdmins { get; set; } = true;

            [JsonProperty("Enable debug logging")]
            public bool DebugLogging { get; set; } = false;

            [JsonProperty("Deferred data save (true = periodic timer, false = save on every change)")]
            public bool DeferredSave { get; set; } = false;

            [JsonProperty("Data save interval (seconds) — used only when Deferred data save is true")]
            public float DataSaveIntervalSeconds { get; set; } = 300f;

            [JsonProperty("Stale record prune age (days, 0 = disabled)")]
            public int StaleRecordPruneAgeDays { get; set; } = 90;

            [JsonProperty("Discord webhook notifications")]
            public DiscordWebhookConfig DiscordWebhook { get; set; } = new DiscordWebhookConfig();
        }

        private class BanGraceConfig
        {
            [JsonProperty("Enabled (recheck visibility after BAN expires)")]
            public bool Enabled { get; set; } = false;

            [JsonProperty("Escalated BAN duration (seconds)")]
            public float EscalatedBanDurationSeconds { get; set; } = 86400f;
        }

        private class DiscordWebhookConfig
        {
            [JsonProperty("Webhook URL")]
            public string Url { get; set; } = string.Empty;

            [JsonProperty("Username")]
            public string Username { get; set; } = "BeginnerGuard";

            [JsonProperty("Notify when private-profile grace period starts")]
            public bool NotifyGraceStarted { get; set; } = false;

            [JsonProperty("Notify when private-profile grace period expires and player is kicked")]
            public bool NotifyGraceExpired { get; set; } = false;

            [JsonProperty("Notify when private-profile warning kick occurs")]
            public bool NotifyWarningKick { get; set; } = false;

            [JsonProperty("Notify when temporary BAN is issued")]
            public bool NotifyBanIssued { get; set; } = false;

            [JsonProperty("Notify when a banned reconnect is blocked")]
            public bool NotifyBannedReconnect { get; set; } = false;

            [JsonProperty("Notify when a BAN expires automatically")]
            public bool NotifyBanExpired { get; set; } = false;

            [JsonProperty("Notify when bg.unban is used")]
            public bool NotifyManualUnban { get; set; } = false;

            [JsonProperty("Notify when an over-limit player is kicked")]
            public bool NotifyOverLimitKick { get; set; } = false;
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
                if (_config == null)
                {
                    LoadDefaultConfig();
                    return;
                }

                ValidateConfig();
            }
            catch
            {
                PrintError("Failed to load config.json — using defaults.");
                LoadDefaultConfig();
                return;
            }

            try
            {
                SaveConfig(); // Persist defaults added by newer plugin versions.
            }
            catch (Exception ex)
            {
                PrintWarning($"Config loaded but could not be updated: {ex.Message}");
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private void ValidateConfig()
        {
            if (_config.DiscordWebhook == null)
                _config.DiscordWebhook = new DiscordWebhookConfig();
            if (_config.BanGrace == null)
                _config.BanGrace = new BanGraceConfig();

            _config.MaxSteamHours = ValidateNonNegative(_config.MaxSteamHours,
                "Max allowed Rust playtime on Steam (hours)", 1000);
            _config.PrivateProfileMaxMinutes = ValidateNonNegative(_config.PrivateProfileMaxMinutes,
                "Private profile: max cumulative server playtime before kick (minutes)", 120);
            _config.CheckIntervalSeconds = ValidatePositive(_config.CheckIntervalSeconds,
                "Steam API periodic check interval (seconds)", 1800f);
            _config.ApiRetryIntervalSeconds = ValidatePositive(_config.ApiRetryIntervalSeconds,
                "Steam API retry interval on failure (seconds)", 1800f);
            _config.OverLimitKickDelaySeconds = ValidateNonNegative(_config.OverLimitKickDelaySeconds,
                "Over-limit player: delay before kick after warning (seconds)", 300f);
            _config.PrivateProfileKickDelaySeconds = ValidateNonNegative(_config.PrivateProfileKickDelaySeconds,
                "Private profile: delay before kick after warning (seconds)", 300f);
            _config.KickCountBeforeBan = ValidateNonNegative(_config.KickCountBeforeBan,
                "Private profile: max warning kicks before BAN", 2);
            _config.BanDurationSeconds = ValidatePositive(_config.BanDurationSeconds,
                "BAN duration (seconds)", 86400f, 315360000f); // 10 years
            _config.BanGrace.EscalatedBanDurationSeconds = ValidatePositive(
                _config.BanGrace.EscalatedBanDurationSeconds,
                "Private profile BAN grace: Escalated BAN duration (seconds)",
                86400f, 315360000f); // 10 years
            _config.DataSaveIntervalSeconds = ValidatePositive(_config.DataSaveIntervalSeconds,
                "Data save interval (seconds)", 300f);
            _config.StaleRecordPruneAgeDays = ValidateNonNegative(_config.StaleRecordPruneAgeDays,
                "Stale record prune age (days, 0 = disabled)", 90, 365000); // 1000 years

            if (string.IsNullOrWhiteSpace(_config.DiscordWebhook.Username))
                _config.DiscordWebhook.Username = "BeginnerGuard";
        }

        private float ValidatePositive(float value, string name, float fallback,
            float maximum = float.MaxValue)
        {
            if (value > 0f && value <= maximum && !float.IsNaN(value) && !float.IsInfinity(value))
                return value;
            PrintWarning($"Invalid config value for '{name}' ({value}); using {fallback}.");
            return fallback;
        }

        private float ValidateNonNegative(float value, string name, float fallback)
        {
            if (value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value)) return value;
            PrintWarning($"Invalid config value for '{name}' ({value}); using {fallback}.");
            return fallback;
        }

        private int ValidateNonNegative(int value, string name, int fallback,
            int maximum = int.MaxValue)
        {
            if (value >= 0 && value <= maximum) return value;
            PrintWarning($"Invalid config value for '{name}' ({value}); using {fallback}.");
            return fallback;
        }

        // ---------------------------------------------------------------
        // Localization  →  oxide/lang/{lang}/BeginnerGuard.json
        // ---------------------------------------------------------------
        protected override void LoadDefaultMessages()
        {
            // English (default)
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PrivateProfile.GraceWarn"]       = "<color=#FFA500>[BeginnerGuard] Your Steam game details or total playtime are not public.\nYou will be kicked in approximately {0} minute(s) if they remain hidden.\nHow to fix: Steam → Profile → Edit Profile → Privacy Settings → set Game details to Public and uncheck 'Always keep my total playtime private even if users can see my game details'.</color>",
                ["PrivateProfile.GraceKickReason"] = "[BeginnerGuard] Kicked: cumulative server playtime limit reached.\nSet Steam Game details to Public and uncheck 'Always keep my total playtime private even if users can see my game details', then reconnect.",
                ["PrivateProfile.WarnKick"]        = "<color=#FFA500>[BeginnerGuard] Your Steam game details or total playtime are hidden!\nPlease make them public within {0}s.\nWarning {1}/{2} — a {3}h BAN will be issued if you exceed this.\nHow to fix: Steam → Profile → Edit Profile → Privacy Settings → Game details: Public; uncheck 'Always keep my total playtime private even if users can see my game details'.</color>",
                ["PrivateProfile.WarnKickReason"]  = "[BeginnerGuard] Kicked: Steam game details or total playtime are hidden.\nSet Game details to Public, uncheck the total-playtime privacy option, and reconnect.",
                ["PrivateProfile.BanKickReason"]   = "[BeginnerGuard] You have been banned for {0} hour(s).\nReason: Steam game details or total playtime remained hidden after repeated warnings.\nSet Game details to Public and uncheck the total-playtime privacy option before reconnecting.",
                ["PrivateProfile.EscalatedBanKickReason"] = "[BeginnerGuard] Your BAN has been extended to {0} hour(s).\nYour Steam playtime was still unavailable after the previous BAN expired.\nSteam → Profile → Edit Profile → Privacy Settings → Game details: Public; uncheck 'Always keep my total playtime private even if users can see my game details'.",
                ["PrivateProfile.BanConnectKick"]  = "[BeginnerGuard] You are banned. Ban expires in: {0}h {1}m\nBefore reconnecting, set Steam Game details to Public and uncheck 'Always keep my total playtime private even if users can see my game details'.",
                ["OverLimit.Warn"]                 = "<color=#FFA500>[BeginnerGuard] This is a beginner-only server.\nYour Rust playtime on Steam: {0}h (limit: {1}h).\nYou will be kicked in {2}s. Please find a server that matches your experience level.</color>",
                ["OverLimit.KickReason"]           = "[BeginnerGuard] Kicked: playtime too high ({0}h / limit {1}h).\nThis server is for beginners only. Thanks for understanding!",
            }, this);

            // Japanese
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PrivateProfile.GraceWarn"]       = "<color=#FFA500>[BeginnerGuard] Steamのゲーム詳細または総プレイ時間が公開されていません。\nこのまま確認できない場合、約{0}分後にキックされます。\n修正方法: Steam → プロフィール → プロフィールを編集 → プライバシー設定 →「ゲームの詳細」を公開し、「ゲームの詳細が公開中でも総プレイ時間を常に非公開にする」のチェックを外してください。</color>",
                ["PrivateProfile.GraceKickReason"] = "[BeginnerGuard] キック: サーバー滞在時間の上限に達しました。\nSteamの「ゲームの詳細」を公開し、総プレイ時間を常に非公開にする設定をオフにしてから再接続してください。",
                ["PrivateProfile.WarnKick"]        = "<color=#FFA500>[BeginnerGuard] Steamのゲーム詳細または総プレイ時間を確認できません！\n{0}秒以内に公開してください。\n警告 {1}/{2} — 超過した場合は{3}時間のBANが適用されます。\n修正方法: Steam → プロフィール → プロフィールを編集 → プライバシー設定 →「ゲームの詳細」を公開し、「ゲームの詳細が公開中でも総プレイ時間を常に非公開にする」のチェックを外してください。</color>",
                ["PrivateProfile.WarnKickReason"]  = "[BeginnerGuard] キック: Steamのゲーム詳細または総プレイ時間を確認できません。\n「ゲームの詳細」を公開し、総プレイ時間を常に非公開にする設定をオフにしてから再接続してください。",
                ["PrivateProfile.BanKickReason"]   = "[BeginnerGuard] {0}時間のBANが適用されました。\n理由: 警告後もSteamのゲーム詳細または総プレイ時間を確認できませんでした。\n「ゲームの詳細」を公開し、総プレイ時間を常に非公開にする設定をオフにしてください。",
                ["PrivateProfile.EscalatedBanKickReason"] = "[BeginnerGuard] BANが{0}時間に延長されました。\n前回のBAN終了後もSteamの総プレイ時間を確認できませんでした。\nSteam → プロフィール → プロフィールを編集 → プライバシー設定 →「ゲームの詳細」を公開し、「ゲームの詳細が公開中でも総プレイ時間を常に非公開にする」のチェックを外してください。",
                ["PrivateProfile.BanConnectKick"]  = "[BeginnerGuard] BANされています。解除まで: {0}時間{1}分\n再接続前にSteamの「ゲームの詳細」を公開し、「ゲームの詳細が公開中でも総プレイ時間を常に非公開にする」のチェックを外してください。",
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
            // 0 = no active BAN cycle, 1 = initial BAN issued, 2 = escalated BAN cycle
            public int    PrivateBanStage       { get; set; } = 0;
            // BannedUntil stored as UTC ticks; 0 = not banned
            public long   BannedUntilTicks      { get; set; } = 0;
            // LastSteamCheck stored as UTC ticks; 0 = never checked
            public long   LastSteamCheckTicks   { get; set; } = 0;
            // LastSeen stored as UTC ticks of last disconnect; 0 = no disconnect recorded yet
            public long   LastSeenTicks         { get; set; } = 0;

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
            [JsonIgnore] public DateTime LastSeen
            {
                get => LastSeenTicks > 0 ? new DateTime(LastSeenTicks, DateTimeKind.Utc) : DateTime.MinValue;
                set => LastSeenTicks = value.Ticks;
            }
        }

        private class StoredData
        {
            public Dictionary<string, PlayerRecord> Players { get; set; }
                = new Dictionary<string, PlayerRecord>();
        }

        private class DiscordWebhookPayload
        {
            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }

            [JsonProperty("allowed_mentions")]
            public DiscordAllowedMentions AllowedMentions { get; set; } = new DiscordAllowedMentions();
        }

        private class DiscordAllowedMentions
        {
            [JsonProperty("parse")]
            public string[] Parse { get; set; } = new string[0];
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
            record.SteamId = sid; // Backfill records created by older versions.
            record.DisplayName = player.displayName;
            return record;
        }

        // ---------------------------------------------------------------
        // Timers
        // ---------------------------------------------------------------
        private Timer _periodicCheckTimer;
        private Timer _dataSaveTimer;
        private bool  _dataDirty = false;
        private readonly Dictionary<string, Timer> _pendingKickTimers
            = new Dictionary<string, Timer>();
        private readonly Dictionary<string, Timer> _steamRetryTimers
            = new Dictionary<string, Timer>();
        private readonly Dictionary<string, long> _steamChecksInFlight
            = new Dictionary<string, long>();
        private long _nextSteamCheckToken = 0;

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

            if (!HasUsableSteamApiKey())
                PrintError("Steam API key is not configured. Player checks will remain disabled until a valid key is set.");

            if (AnyDiscordNotificationEnabled() && !HasUsableDiscordWebhook())
                PrintWarning("Discord notifications are enabled, but Webhook URL is missing or invalid.");
        }

        private void OnServerInitialized()
        {
            RecoverOfflineSessions();
            PruneStaleRecords();

            // On hot reload, already-connected players do not necessarily emit
            // OnPlayerConnected again. Initialise and check them immediately.
            foreach (var player in new List<BasePlayer>(BasePlayer.activePlayerList))
                OnPlayerConnected(player);

            _periodicCheckTimer = timer.Every(_config.CheckIntervalSeconds, () =>
            {
                DebugLog("Periodic Steam check triggered.");
                foreach (var player in BasePlayer.activePlayerList)
                    FetchAndProcessSteamHours(player);
            });
            Puts($"Periodic check scheduled every {_config.CheckIntervalSeconds}s.");

            if (_config.DeferredSave)
            {
                _dataSaveTimer = timer.Every(_config.DataSaveIntervalSeconds, FlushDataIfDirty);
                Puts($"Data save: deferred (interval {_config.DataSaveIntervalSeconds}s).");
            }
            else
            {
                Puts("Data save: immediate (deferred save disabled).");
            }
        }

        private void Unload()
        {
            SnapshotActiveSessions();
            _periodicCheckTimer?.Destroy();
            _dataSaveTimer?.Destroy();
            foreach (var t in _pendingKickTimers.Values) t?.Destroy();
            foreach (var t in _steamRetryTimers.Values) t?.Destroy();
            _pendingKickTimers.Clear();
            _steamRetryTimers.Clear();
            _steamChecksInFlight.Clear();
            FlushDataIfDirty();
            Puts("BeginnerGuard unloaded.");
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (IsExempt(player))
            {
                CancelPendingKick(player.UserIDString);
                CancelSteamRetry(player.UserIDString);
                InvalidateSteamCheck(player.UserIDString);
                DebugLog($"{player.displayName} ({player.UserIDString}) is exempt — skipping.");
                return;
            }

            var record = GetOrCreateRecord(player);

            // BAN check
            if (record.BannedUntil.HasValue)
            {
                // Enrol active or expired BANs created before this feature was
                // enabled into the initial stage on their next connection.
                if (_config.BanGrace.Enabled && record.PrivateBanStage == 0)
                {
                    record.PrivateBanStage = 1;
                    SaveData();
                }

                if (DateTime.UtcNow < record.BannedUntil.Value)
                {
                    var rem = record.BannedUntil.Value - DateTime.UtcNow;
                    var totalMinutes = Math.Max(1L, (long)Math.Ceiling(rem.TotalMinutes));
                    var remainingHours = totalMinutes / 60;
                    var remainingMinutes = totalMinutes % 60;
                    DebugLog($"{player.displayName} is BAN'd for another {rem.TotalMinutes:F0} min.");
                    NotifyDiscord(_config.DiscordWebhook.NotifyBannedReconnect,
                        "Banned reconnect blocked", player,
                        $"Remaining: {remainingHours}h {remainingMinutes}m");
                    KickPlayer(player,
                        GetMsg("PrivateProfile.BanConnectKick", player, remainingHours, remainingMinutes));
                    return;
                }
                // Expired — either reset normally or retain the BAN stage so
                // the Steam response can decide whether escalation is needed.
                bool recheckForEscalation = _config.BanGrace.Enabled &&
                                            record.PrivateBanStage > 0;
                DebugLog($"BAN expired for {player.displayName} — " +
                         (recheckForEscalation ? "rechecking visibility." : "lifting automatically."));
                record.BannedUntil = null;
                if (!recheckForEscalation)
                {
                    record.PrivateKickCount = 0;
                    record.PrivateBanStage  = 0;
                }
                SaveData();  // flush BAN lift immediately
                NotifyDiscord(_config.DiscordWebhook.NotifyBanExpired,
                    "BAN expired automatically", player,
                    recheckForEscalation
                        ? "Steam playtime visibility will be rechecked before the BAN cycle is reset."
                        : "The player will now be checked normally.");
            }

            record.LastJoinTime = DateTime.UtcNow;
            MarkDirty();

            DebugLog($"{player.displayName} connected — starting Steam check.");
            FetchAndProcessSteamHours(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            CancelPendingKick(player.UserIDString);
            CancelSteamRetry(player.UserIDString);
            InvalidateSteamCheck(player.UserIDString);

            if (!_data.Players.TryGetValue(player.UserIDString, out var record)) return;
            if (!record.LastJoinTime.HasValue) return;

            double session = Math.Max(0.0,
                (DateTime.UtcNow - record.LastJoinTime.Value).TotalMinutes);
            record.ServerPlaytimeMinutes += session;
            record.LastJoinTime           = null;
            record.LastSeen               = DateTime.UtcNow;

            DebugLog($"{player.displayName} disconnected — session {session:F1} min, " +
                     $"cumulative {record.ServerPlaytimeMinutes:F1} min.");
            MarkDirty();
        }

        private void RecoverOfflineSessions()
        {
            var activeIds = new HashSet<string>();
            foreach (var player in BasePlayer.activePlayerList)
                activeIds.Add(player.UserIDString);

            bool changed = false;
            foreach (var entry in _data.Players)
            {
                var record = entry.Value;
                if (!record.LastJoinTime.HasValue || activeIds.Contains(entry.Key)) continue;

                // A persisted join without an active player means the previous
                // shutdown did not receive a disconnect hook. Do not count
                // server downtime as playtime; retain the join as last-seen data.
                if (record.LastSeen == DateTime.MinValue || record.LastSeen < record.LastJoinTime.Value)
                    record.LastSeen = record.LastJoinTime.Value;
                record.LastJoinTime = null;
                changed = true;
            }

            if (changed) SaveData();
        }

        private void SnapshotActiveSessions()
        {
            bool changed = false;
            var now = DateTime.UtcNow;

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!_data.Players.TryGetValue(player.UserIDString, out var record)) continue;
                if (!record.LastJoinTime.HasValue) continue;

                record.ServerPlaytimeMinutes += Math.Max(0.0,
                    (now - record.LastJoinTime.Value).TotalMinutes);
                record.LastJoinTime = null;
                record.LastSeen = now;
                changed = true;
            }

            if (!changed) return;
            SaveData();
            _dataDirty = false;
        }

        // ---------------------------------------------------------------
        // Steam API
        // ---------------------------------------------------------------
        private void FetchAndProcessSteamHours(BasePlayer player)
        {
            var sid = player.UserIDString;
            if (IsExempt(player))
            {
                CancelPendingKick(sid);
                CancelSteamRetry(sid);
                InvalidateSteamCheck(sid);
                return;
            }

            if (!HasUsableSteamApiKey()) return;

            if (_steamChecksInFlight.ContainsKey(sid))
            {
                DebugLog($"Steam check already in progress for {player.displayName} ({sid}) — skipping duplicate.");
                return;
            }

            var checkToken = ++_nextSteamCheckToken;
            _steamChecksInFlight[sid] = checkToken;

            var url = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/" +
                      $"?key={_config.SteamApiKey}&steamid={sid}" +
                      $"&include_appinfo=false&appids_filter[0]={RustAppId}&format=json";

            DebugLog($"Fetching Steam hours for {player.displayName} ({sid})...");

            webrequest.Enqueue(url, null, (code, response) =>
            {
                if (!_steamChecksInFlight.TryGetValue(sid, out var activeToken) || activeToken != checkToken)
                {
                    DebugLog($"Ignoring stale Steam response for {player.displayName} ({sid}).");
                    return;
                }
                _steamChecksInFlight.Remove(sid);

                if (!player.IsConnected)
                {
                    DebugLog($"{player.displayName} disconnected before API response — ignoring.");
                    return;
                }

                if (IsExempt(player))
                {
                    CancelPendingKick(sid);
                    CancelSteamRetry(sid);
                    return;
                }

                var record = GetOrCreateRecord(player);

                // --- API failure ---
                if (code != 200 || string.IsNullOrEmpty(response))
                {
                    PrintWarning($"[BeginnerGuard] Steam API failed (HTTP {code}) for {player.displayName}. " +
                                 $"Retrying in {_config.ApiRetryIntervalSeconds}s.");
                    ScheduleSteamRetry(player);
                    return;
                }

                CancelSteamRetry(sid);
                DebugLog($"Steam API response received for {player.displayName} (HTTP {code}).");

                try
                {
                    var root    = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                    var respObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                      root["response"].ToString());

                    // game_count missing or 0 → game details/playtime unavailable
                    if (!respObj.ContainsKey("game_count") || respObj["game_count"].ToString() == "0")
                    {
                        DebugLog($"{player.displayName}: game_count=0 or missing → playtime unavailable.");
                        HandlePrivateProfile(player, record);
                        return;
                    }

                    var games = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                                    respObj["games"].ToString());

                    if (games == null || games.Count == 0)
                    {
                        DebugLog($"{player.displayName}: games list empty → playtime unavailable.");
                        HandlePrivateProfile(player, record);
                        return;
                    }

                    int minutesPlayed;
                    if (!TryGetRustPlaytimeMinutes(games, out minutesPlayed))
                    {
                        DebugLog($"{player.displayName}: Rust entry or playtime is missing.");
                        HandlePrivateProfile(player, record);
                        return;
                    }

                    // Steam may return playtime_forever=0 when total playtime is
                    // hidden even though the profile, game, and achievements are
                    // public. A genuinely new player is handled safely by the
                    // existing grace period until Steam reports a positive value.
                    if (minutesPlayed <= 0)
                    {
                        DebugLog($"{player.displayName}: Rust playtime is 0 → treating as unavailable.");
                        HandlePrivateProfile(player, record);
                        return;
                    }

                    int hours = minutesPlayed / 60;
                    bool overLimit = minutesPlayed > _config.MaxSteamHours * 60L;
                    record.SteamTotalHours  = hours;
                    record.IsProfilePrivate = false;
                    record.LastSteamCheck   = DateTime.UtcNow;
                    // Publishing playtime completes any private-profile BAN cycle.
                    record.PrivateKickCount = 0;
                    record.PrivateBanStage  = 0;
                    MarkDirty();

                    Puts($"[BeginnerGuard] {player.displayName} — Steam Rust hours: {hours}h " +
                         $"(limit: {_config.MaxSteamHours}h)");

                    if (overLimit)
                        HandleOverLimitPlayer(player, record, hours);
                    else
                    {
                        CancelPendingKick(sid);
                        DebugLog($"{player.displayName} is within the hour limit — allowed.");
                    }
                }
                catch (Exception ex)
                {
                    PrintError($"[BeginnerGuard] Failed to parse Steam API response for " +
                               $"{player.displayName}: {ex.Message}. " +
                               $"Retrying in {_config.ApiRetryIntervalSeconds}s.");
                    ScheduleSteamRetry(player);
                }

            }, this);
        }

        private bool TryGetRustPlaytimeMinutes(
            List<Dictionary<string, object>> games, out int minutesPlayed)
        {
            minutesPlayed = 0;

            foreach (var game in games)
            {
                object appIdValue;
                if (!game.TryGetValue("appid", out appIdValue) ||
                    Convert.ToInt32(appIdValue) != RustAppId)
                    continue;

                object playtimeValue;
                if (!game.TryGetValue("playtime_forever", out playtimeValue))
                    return false;

                minutesPlayed = Convert.ToInt32(playtimeValue);
                return true;
            }

            return false;
        }

        // ---------------------------------------------------------------
        // Enforcement
        // ---------------------------------------------------------------
        private void HandlePrivateProfile(BasePlayer player, PlayerRecord record)
        {
            record.IsProfilePrivate = true;
            record.LastSteamCheck   = DateTime.UtcNow;
            MarkDirty();

            if (_config.BanGrace.Enabled && record.PrivateBanStage > 0)
            {
                IssuePrivateProfileBan(player, record,
                    _config.BanGrace.EscalatedBanDurationSeconds, true);
                return;
            }

            double currentSession = record.LastJoinTime.HasValue
                ? (DateTime.UtcNow - record.LastJoinTime.Value).TotalMinutes : 0.0;
            double totalMinutes   = record.ServerPlaytimeMinutes + currentSession;

            DebugLog($"{player.displayName}: Steam playtime unavailable. " +
                     $"Cumulative server time = {totalMinutes:F1} min " +
                     $"(limit: {_config.PrivateProfileMaxMinutes} min). " +
                     $"Kick count = {record.PrivateKickCount}/{_config.KickCountBeforeBan}.");

            if (totalMinutes < _config.PrivateProfileMaxMinutes)
            {
                if (HasPendingKick(player.UserIDString)) return;

                // Still within grace period — warn and schedule kick at time-limit
                double remaining = _config.PrivateProfileMaxMinutes - totalMinutes;
                SendMsg(player, GetMsg("PrivateProfile.GraceWarn", player, remaining.ToString("F0")));

                NotifyDiscord(_config.DiscordWebhook.NotifyGraceStarted,
                    "Private-profile grace period started", player,
                    $"Server playtime: {totalMinutes:F1}/{_config.PrivateProfileMaxMinutes} min\n" +
                    $"Grace remaining: {remaining:F0} min");

                ScheduleKick(player, (float)(remaining * 60f),
                    GetMsg("PrivateProfile.GraceKickReason", player),
                    () => NotifyDiscord(_config.DiscordWebhook.NotifyGraceExpired,
                        "Private-profile grace period expired", player,
                        $"Server playtime limit: {_config.PrivateProfileMaxMinutes} min\nAction: kicked"));
                return;
            }

            // Over the cumulative limit
            if (HasPendingKick(player.UserIDString)) return;

            if (record.PrivateKickCount >= _config.KickCountBeforeBan)
            {
                IssuePrivateProfileBan(player, record, _config.BanDurationSeconds, false);
            }
            else
            {
                // Warning kick
                record.PrivateKickCount++;
                int warningNumber = record.PrivateKickCount;
                MarkDirty();

                SendMsg(player, GetMsg("PrivateProfile.WarnKick", player,
                    _config.PrivateProfileKickDelaySeconds.ToString("F0"),
                    warningNumber,
                    _config.KickCountBeforeBan,
                    (_config.BanDurationSeconds / 3600).ToString("F0")));

                ScheduleKick(player, _config.PrivateProfileKickDelaySeconds,
                    GetMsg("PrivateProfile.WarnKickReason", player),
                    () => NotifyDiscord(_config.DiscordWebhook.NotifyWarningKick,
                        "Private-profile warning kick", player,
                        $"Warning: {warningNumber}/{_config.KickCountBeforeBan}\nAction: kicked"));
            }
        }

        private void IssuePrivateProfileBan(BasePlayer player, PlayerRecord record,
            float durationSeconds, bool escalated)
        {
            record.BannedUntil      = DateTime.UtcNow.AddSeconds(durationSeconds);
            record.PrivateKickCount = 0;
            record.PrivateBanStage  = _config.BanGrace.Enabled ? (escalated ? 2 : 1) : 0;
            SaveData();  // flush BAN immediately

            double banHours = durationSeconds / 3600.0;
            string stage = escalated ? "Escalated temporary BAN issued" : "Temporary BAN issued";
            string reason = escalated
                ? "Steam playtime remained unavailable after the previous BAN expired"
                : "Steam playtime unavailable after warnings";

            Puts($"[BeginnerGuard] {(escalated ? "Escalated BAN" : "BAN")} issued to " +
                 $"{player.displayName} ({player.UserIDString}) for {banHours:F0}h — {reason}.");
            NotifyDiscord(_config.DiscordWebhook.NotifyBanIssued,
                stage, player, $"Duration: {banHours:F0}h\nReason: {reason}");
            KickPlayer(player, GetMsg(escalated
                    ? "PrivateProfile.EscalatedBanKickReason"
                    : "PrivateProfile.BanKickReason",
                player, banHours.ToString("F0")));
        }

        private void HandleOverLimitPlayer(BasePlayer player, PlayerRecord record, int hours)
        {
            if (HasPendingKick(player.UserIDString)) return;

            SendMsg(player, GetMsg("OverLimit.Warn", player,
                hours, _config.MaxSteamHours, _config.OverLimitKickDelaySeconds.ToString("F0")));

            ScheduleKick(player, _config.OverLimitKickDelaySeconds,
                GetMsg("OverLimit.KickReason", player, hours, _config.MaxSteamHours),
                () => NotifyDiscord(_config.DiscordWebhook.NotifyOverLimitKick,
                    "Steam playtime limit exceeded", player,
                    $"Steam Rust playtime: {hours}h (limit: {_config.MaxSteamHours}h)\nAction: kicked"));
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

        private void ScheduleKick(BasePlayer player, float delaySec, string reason,
            Action beforeKick = null)
        {
            var sid = player.UserIDString;
            if (HasPendingKick(sid)) return;
            DebugLog($"Kick scheduled for {player.displayName} in {delaySec}s.");
            _pendingKickTimers[sid] = timer.Once(delaySec, () =>
            {
                _pendingKickTimers.Remove(sid);
                if (player.IsConnected && !IsExempt(player))
                {
                    beforeKick?.Invoke();
                    KickPlayer(player, reason);
                }
            });
        }

        private bool HasPendingKick(string steamId)
        {
            return _pendingKickTimers.ContainsKey(steamId);
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

        private void ScheduleSteamRetry(BasePlayer player)
        {
            var sid = player.UserIDString;
            if (_steamRetryTimers.ContainsKey(sid)) return;

            _steamRetryTimers[sid] = timer.Once(_config.ApiRetryIntervalSeconds, () =>
            {
                _steamRetryTimers.Remove(sid);
                if (player.IsConnected) FetchAndProcessSteamHours(player);
            });
        }

        private void CancelSteamRetry(string steamId)
        {
            if (!_steamRetryTimers.TryGetValue(steamId, out var retryTimer)) return;
            retryTimer?.Destroy();
            _steamRetryTimers.Remove(steamId);
        }

        private void InvalidateSteamCheck(string steamId)
        {
            _steamChecksInFlight.Remove(steamId);
        }

        private bool HasUsableSteamApiKey()
        {
            return !string.IsNullOrWhiteSpace(_config.SteamApiKey) &&
                   _config.SteamApiKey != "YOUR_STEAM_API_KEY_HERE";
        }

        private bool AnyDiscordNotificationEnabled()
        {
            var webhook = _config?.DiscordWebhook;
            return webhook != null &&
                (webhook.NotifyGraceStarted || webhook.NotifyGraceExpired ||
                 webhook.NotifyWarningKick || webhook.NotifyBanIssued ||
                 webhook.NotifyBannedReconnect || webhook.NotifyBanExpired ||
                 webhook.NotifyManualUnban || webhook.NotifyOverLimitKick);
        }

        private bool HasUsableDiscordWebhook()
        {
            var url = _config?.DiscordWebhook?.Url;
            if (string.IsNullOrWhiteSpace(url)) return false;

            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri) &&
                   uri.Scheme == Uri.UriSchemeHttps;
        }

        private void NotifyDiscord(bool enabled, string stage, BasePlayer player, string details)
        {
            NotifyDiscord(enabled, stage, player?.displayName ?? "Unknown",
                player?.UserIDString ?? "unknown", details);
        }

        private void NotifyDiscord(bool enabled, string stage, string displayName,
            string steamId, string details)
        {
            if (!enabled || !HasUsableDiscordWebhook()) return;

            var payload = new DiscordWebhookPayload
            {
                Username = _config.DiscordWebhook.Username,
                Content = $"**BeginnerGuard — {stage}**\n" +
                          $"Player: {displayName} ({steamId})\n" +
                          details
            };
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            };

            webrequest.Enqueue(_config.DiscordWebhook.Url,
                JsonConvert.SerializeObject(payload), (code, response) =>
                {
                    if (code >= 200 && code < 300)
                    {
                        DebugLog($"Discord webhook sent: {stage} for {steamId}.");
                        return;
                    }

                    PrintWarning($"Discord webhook failed for '{stage}' " +
                                 $"(HTTP {code}): {response}");
                }, this, Oxide.Core.Libraries.RequestMethod.POST, headers);
        }

        private void MarkDirty()
        {
            if (_config.DeferredSave)
                _dataDirty = true;
            else
                SaveData();
        }

        private void FlushDataIfDirty()
        {
            if (!_dataDirty) return;
            SaveData();
            _dataDirty = false;
        }

        private void PruneStaleRecords()
        {
            if (_config.StaleRecordPruneAgeDays <= 0) return;

            var cutoff   = DateTime.UtcNow.AddDays(-_config.StaleRecordPruneAgeDays);
            var toRemove = new List<string>();
            int migrated = 0;

            foreach (var kv in _data.Players)
            {
                var r = kv.Value;
                if (r.LastJoinTime.HasValue)                                    continue; // online now
                if (r.BannedUntil.HasValue && r.BannedUntil.Value > DateTime.UtcNow) continue; // still banned
                if (r.LastSeenTicks == 0)
                {
                    // Give legacy records one full retention period instead of
                    // exempting them from pruning forever.
                    r.LastSeen = DateTime.UtcNow;
                    migrated++;
                    continue;
                }
                if (r.LastSeen > cutoff)                                        continue; // seen recently
                toRemove.Add(kv.Key);
            }

            foreach (var sid in toRemove)
                _data.Players.Remove(sid);

            if (toRemove.Count > 0 || migrated > 0)
            {
                SaveData();
                if (toRemove.Count > 0)
                    Puts($"[BeginnerGuard] Pruned {toRemove.Count} stale record(s) older than {_config.StaleRecordPruneAgeDays} days.");
                if (migrated > 0)
                    Puts($"[BeginnerGuard] Initialised last-seen timestamps for {migrated} legacy record(s).");
            }
            else
            {
                DebugLog("Prune: no stale records found.");
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
                "bg.prune                   Remove stale records older than configured age\n" +
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
                    $"Playtime unavailable: {r.IsProfilePrivate}\n" +
                    $"Server playtime     : {r.ServerPlaytimeMinutes:F1} min\n" +
                    $"Kick count          : {r.PrivateKickCount} / {_config.KickCountBeforeBan}\n" +
                    $"Private BAN stage   : {r.PrivateBanStage}\n" +
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
                bool hadActiveBan = r.BannedUntil.HasValue && r.BannedUntil.Value > DateTime.UtcNow;
                r.BannedUntil      = null;
                r.PrivateKickCount = 0;
                r.PrivateBanStage  = 0;
                SaveData();
                arg.ReplyWith($"[BeginnerGuard] BAN lifted for {r.DisplayName} ({sid}).");
                Puts($"[BeginnerGuard] BAN manually lifted for {r.DisplayName} ({sid}).");

                NotifyDiscord(_config.DiscordWebhook.NotifyManualUnban,
                    "BAN manually lifted", r.DisplayName, sid,
                    $"Active BAN before command: {(hadActiveBan ? "yes" : "no")}\n" +
                    "BAN stage and warning kick count reset to 0.");
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
            if (IsExempt(player))
            {
                arg.ReplyWith($"{player.displayName} is exempt from BeginnerGuard checks.");
                return;
            }
            if (!HasUsableSteamApiKey())
            {
                arg.ReplyWith("[BeginnerGuard] Steam API key is not configured.");
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
                string displayName = r.DisplayName;
                CancelPendingKick(sid);
                CancelSteamRetry(sid);
                InvalidateSteamCheck(sid);
                _data.Players.Remove(sid);

                BasePlayer onlinePlayer = null;
                if (ulong.TryParse(sid, out var uid))
                    onlinePlayer = BasePlayer.FindByID(uid);

                if (onlinePlayer != null && onlinePlayer.IsConnected)
                {
                    var freshRecord = GetOrCreateRecord(onlinePlayer);
                    freshRecord.LastJoinTime = DateTime.UtcNow;
                }

                SaveData();
                arg.ReplyWith($"[BeginnerGuard] Record reset for {displayName} ({sid}).");
                Puts($"[BeginnerGuard] Record manually reset for {displayName} ({sid}).");

                if (onlinePlayer != null && onlinePlayer.IsConnected)
                    FetchAndProcessSteamHours(onlinePlayer);
            }
            else
            {
                arg.ReplyWith($"No record found for {sid}.");
            }
        }

        [ConsoleCommand("bg.prune")]
        private void CmdPrune(ConsoleSystem.Arg arg)
        {
            if (!HasAdminPerm(arg)) return;
            int before = _data.Players.Count;
            PruneStaleRecords();
            int after  = _data.Players.Count;
            arg.ReplyWith($"[BeginnerGuard] Prune complete — removed {before - after} record(s), {after} remain.");
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
