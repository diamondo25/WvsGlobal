using System;
using System.Linq;
using log4net;
using StackExchange.Redis;

namespace WvsBeta.Common
{
    public class RedisBackend
    {
        private IConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _db;
        private static ILog _log = LogManager.GetLogger("OnlinePlayerManager");

        public static RedisBackend Instance { get; private set; }

        private static readonly TimeSpan _onlineTimeout = TimeSpan.FromSeconds(180); // 3 minutes
        private static readonly TimeSpan _migrateTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan _ccSaveTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _onlineCountTimeout = TimeSpan.FromSeconds(60);

        RedisBackend(ConfigReader configReader)
        {
            var cfg = configReader["redis"];
            if (cfg == null)
            {
                _log.Warn("No redis configuration. Falling back to...nothing.");
            }
            else
            {
                _connectionMultiplexer = ConnectionMultiplexer.Connect(cfg.GetString());
                _db = _connectionMultiplexer.GetDatabase();
            }
        }

        public static void Init(ConfigReader configReader)
        {
            Instance = new RedisBackend(configReader);
        }

        private static string GetMutedCharacterIdKeyName(int characterId) => "muted-" + characterId;
        private static string GetUserIDKeyName(int userId) => "online-player-" + userId;
        private static string GetMigratingCharacterIdKeyName(int characterId) => "migrating-" + characterId;
        private static string GetUndercoverKeyName(int characterId) => "undercover-" + characterId;
        private static string GetImitateKeyName(int characterId) => "imitate-" + characterId;
        private static string GetCCProcessingKeyName(int characterId) => "processing-cc-" + characterId;

        private static string GetNonGameHackDetectedKeyName(int userId) => "hack-detected-" + userId;

        public static int GetOnlineId(int world, int channel)
        {
            return 20000 + (world * 100) + channel;
        }

        public void SetPlayerOnline(int userId, int onlineId)
        {
            if (_db == null) return;
            var key = GetUserIDKeyName(userId);

            _db.StringSet(
                key,
                "" + onlineId,
                _onlineTimeout,
                When.Always,
                CommandFlags.FireAndForget
            );
        }

        public void SetPlayerCCIsBeingProcessed(int characterId)
        {
            if (_db == null) return;
            _db.StringSet(
                GetCCProcessingKeyName(characterId),
                "",
                _ccSaveTimeout,
                When.Always,
                CommandFlags.FireAndForget
            );
        }

        public void RemovePlayerCCIsBeingProcessed(int characterId)
        {
            if (_db == null) return;
            if (false)
            {
                // Free the key
                _db.KeyDelete(GetCCProcessingKeyName(characterId), CommandFlags.FireAndForget);
            }
            else
            {
                // Expire in 2 seconds
                _db.StringSet(
                    GetCCProcessingKeyName(characterId),
                    "",
                    TimeSpan.FromSeconds(1), // 2 second delay 
                    When.Always,
                    CommandFlags.FireAndForget
                );
            }
        }

        public bool HoldoffPlayerConnection(int characterId)
        {
            if (_db == null) return false;
            return _db.KeyExists(GetCCProcessingKeyName(characterId));
        }

        public void RemovePlayerOnline(int userId)
        {
            if (_db == null) return;
            _db.KeyDelete(GetUserIDKeyName(userId), CommandFlags.FireAndForget);
        }

        public bool IsPlayerOnline(int userId)
        {
            if (_db == null) return false;

            return _db.KeyExists(GetUserIDKeyName(userId));
        }

        public void SetMigratingPlayer(int characterId)
        {
            if (_db == null) return;
            var key = GetMigratingCharacterIdKeyName(characterId);

            _db.StringSet(
                key,
                "",
                _migrateTimeout,
                When.Always,
                CommandFlags.FireAndForget
            );
        }

        public bool PlayerIsMigrating(int characterId, bool fallbackValue)
        {
            if (_db == null) return fallbackValue;
            // Just delete the key; this is atomic so we are sure the person cannot login twice
            return _db.KeyDelete(GetMigratingCharacterIdKeyName(characterId));
        }

        public void SetPlayerOnlineCount(int world, int channel, int count)
        {
            _db?.StringSet(
                $"online-players-{world}-{channel}",
                count,
                _onlineCountTimeout,
                When.Always,
                CommandFlags.FireAndForget
            );
        }

        public void MuteCharacter(int fucker, int characterId, int hours)
        {
            _db?.StringSet(
                GetMutedCharacterIdKeyName(characterId),
                fucker.ToString(),
                TimeSpan.FromHours(hours),
                When.Always,
                CommandFlags.FireAndForget
            );
        }

        public void UnmuteCharacter(int characterId)
        {
            _db?.KeyDelete(GetMutedCharacterIdKeyName(characterId));
        }

        public TimeSpan? GetCharacterMuteTime(int characterId)
        {
            return _db?.KeyTimeToLive(GetMutedCharacterIdKeyName(characterId));
        }

        public bool IsUndercover(int characterId)
        {
            return _db?.KeyExists(
                GetUndercoverKeyName(characterId)
            ) ?? false;
        }

        public void SetUndercover(int characterId, bool undercover)
        {
            if (undercover == false)
            {
                _db?.KeyDelete(GetUndercoverKeyName(characterId));
            }
            else
            {
                _db?.StringSet(
                    GetUndercoverKeyName(characterId),
                    "",
                    null,
                    When.Always,
                    CommandFlags.FireAndForget
                );
            }
        }

        public int? GetImitateID(int characterId)
        {
            var possibleId = _db?.StringGet(GetImitateKeyName(characterId));
            if (possibleId != null && int.TryParse(possibleId.Value, out int id)) return id;
            return null;
        }

        public void SetImitateID(int characterId, int victimId)
        {
            if (victimId == 0)
                _db?.KeyDelete(GetImitateKeyName(characterId));
            else
                _db?.StringSet(GetImitateKeyName(characterId), victimId);
        }

        [Flags]
        public enum HackKind
        {
            MemoryEdits = 1,
            Speedhack = 2
        }

        public bool TryGetNonGameHackDetect(int userId, out HackKind hk)
        {
            var key = GetNonGameHackDetectedKeyName(userId);
            var res = Enum.TryParse(_db?.StringGet(key) ?? "", out hk);
            if (res)
            {
                _db?.KeyDelete(key);
            }
            return res;
        }

        public void RegisterNonGameHackDetection(int userId, HackKind hk)
        {
            _db?.StringSet(GetNonGameHackDetectedKeyName(userId), hk.ToString());
        }
    }
}