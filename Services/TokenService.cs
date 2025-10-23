using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.Json;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Penumbra.Plugin;
using static Penumbra.Services.PlayerService;

namespace Penumbra.Services;
internal static class TokenService
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _tokens = _tokensConfig.TokenSystem;

    static readonly int _tokenRate = _tokensConfig.TokenRate;
    static readonly int _tokenRatio = _tokensConfig.TokenRatio;
    static readonly int _dailyQuantity = _tokensConfig.DailyQuantity;

    static readonly PrefabGUID _tokenItem = _tokensConfig.TokenItem;
    static readonly PrefabGUID _dailyItem = _tokensConfig.DailyItem;

    static readonly WaitForSeconds _delay = new(_tokensConfig.UpdateInterval);

    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    static bool _initialized = false;
    public static void Initialize()
    {
        if (_initialized)
            return;

        TokensRoutine().Start();
        _initialized = true;
    }
    static IEnumerator TokensRoutine()
    {
        while (true)
        {
            Dictionary<ulong, TokenBlob> updated = [];

            foreach (var kvp in SteamIdPlayerInfoCache)
            {
                var user = kvp.Value.User;
                if (!user.IsConnected)
                    continue;

                ulong steamId = user.PlatformId;

                if (!PlayerTokens.TryGetValue(steamId, out var tokenData))
                {
                    steamId.CreateTokens();
                    continue;
                }

                if (_tokens)
                {
                    tokenData = AccumulateTime(tokenData);
                    updated[steamId] = tokenData;
                }

                yield return null;
            }

            if (updated.Count > 0)
            {
                List<(ulong steamId, TokenBlob blob)> pendingUpdates = new(updated.Count);

                foreach (var entry in updated)
                {
                    pendingUpdates.Add((entry.Key, entry.Value));
                }

                ApplyAndSaveTokenUpdates(pendingUpdates);
            }
            yield return _delay;
        }
    }

    /*
    static IEnumerator TokensRoutine()
    {
        while (true)
        {
            DateTime now = DateTime.Now;
            Dictionary<ulong, TokenBlob> updatedTokens = [];

            foreach (var kvp in SteamIdPlayerInfoCache)
            {
                User user = kvp.Value.User;
                ulong steamId = user.PlatformId;

                if (!user.IsConnected) continue;
                else if (PlayerTokens.TryGetValue(steamId, out var tokenData))
                {
                    TimeSpan timeOnline = now - tokenData.TimeData.TokenTime;
                    int newTokens = tokenData.Tokens + timeOnline.Minutes * _tokenRate;

                    updatedTokens[steamId] = new TokenBlob(newTokens, new TimeBlob
                    {
                        TokenTime = now,
                        LoginTime = tokenData.TimeData.LoginTime
                    });
                }

                yield return null;
            }

            foreach (var tokenData in updatedTokens)
            {
                _playerTokens[tokenData.Key] = tokenData.Value;
            }

            SaveTokens();
            // ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, $"<color=red>Sanguis</color> have been updated, don't forget to redeem them! (.sanguis r)");

            yield return _delay;
        }
    }
    */

    [Serializable]
    public struct TimeBlob
    {
        public DateTime TokenTime { get; set; }
        public DateTime LoginTime { get; set; }

        public TimeBlob(DateTime tokenTime, DateTime loginTime)
        {
            TokenTime = tokenTime;
            LoginTime = loginTime;
        }
    }

    [Serializable]
    public struct TokenBlob
    {
        public int Tokens { get; set; }
        public TimeBlob TimeData { get; set; }

        public TokenBlob(int tokens, TimeBlob timeBlob)
        {
            Tokens = tokens;
            TimeData = timeBlob;
        }
    }
    public static IReadOnlyDictionary<ulong, TokenBlob> PlayerTokens => _playerTokens;
    static ConcurrentDictionary<ulong, TokenBlob> _playerTokens = [];
    public static TokenBlob AccumulateTime(TokenBlob tokenData)
    {
        TimeSpan timeOnline = DateTime.UtcNow - tokenData.TimeData.TokenTime;
        int additionalTokens = timeOnline.Minutes * _tokenRate;

        return new TokenBlob(tokenData.Tokens + additionalTokens, new TimeBlob
        {
            TokenTime = DateTime.UtcNow,
            LoginTime = tokenData.TimeData.LoginTime
        });
    }
    public static bool IsEligibleForDaily(TokenBlob tokenData)
    {
        return DateTime.UtcNow >= tokenData.TimeData.LoginTime.AddDays(1);
    }
    public static TimeSpan TimeUntilNextDaily(TokenBlob tokenData)
    {
        DateTime nextEligible = tokenData.TimeData.LoginTime.AddDays(1);
        return nextEligible - DateTime.UtcNow;
    }
    public static bool TryGiveDaily(ulong steamId, User user, Entity characterEntity)
    {
        if (!characterEntity.Exists())
            return false;

        if (!PlayerTokens.TryGetValue(steamId, out var tokenData)
            || !IsEligibleForDaily(tokenData))
        {
            return false;
        }

        string itemName = _dailyItem.GetLocalizedName();
        string msg = $"You've received <color=#00FFFF>{itemName}</color>x<color=white>{_dailyQuantity}</color> for logging in today!";
        bool placed = ServerGameManager.TryAddInventoryItem(characterEntity, _dailyItem, _dailyQuantity);

        FixedString512Bytes message = placed
            ? new FixedString512Bytes(msg)
            : new FixedString512Bytes($"{msg} It dropped on the ground because your inventory was full.");

        if (!placed)
            InventoryUtilitiesServer.CreateDropItem(EntityManager, characterEntity, _dailyItem, _dailyQuantity, Entity.Null);

        ServerChatUtils.SendSystemMessageToClient(EntityManager, user, ref message);

        tokenData.TimeData = new TimeBlob(tokenData.TimeData.TokenTime, DateTime.UtcNow);
        steamId.UpdateAndSaveTokens(tokenData);
        return true;
    }
    public static void UpdateAndSaveTokens(this ulong steamId, TokenBlob tokenBlob)
    {
        ApplyAndSaveTokenUpdates([(steamId, tokenBlob)]);
    }
    public static void UpdateTokens(this ulong steamId, TokenBlob tokenBlob)
    {
        _playerTokens[steamId] = tokenBlob;
    }
    public static void CreateTokens(this ulong steamId)
    {
        DateTime now = DateTime.UtcNow;
        TokenBlob tokenBlob = new(0, new TimeBlob(now, now.AddDays(-1)));

        UpdateAndSaveTokens(steamId, tokenBlob);
    }
    public static void LoadTokens()
    {
        if (!File.Exists(TokensPath))
            return;

        _playerTokens = JsonSerializer.Deserialize<ConcurrentDictionary<ulong, TokenBlob>>
                       (File.ReadAllText(TokensPath)) ?? [];
    }
    public static void ApplyAndSaveTokenUpdates(IEnumerable<(ulong steamId, TokenBlob blob)> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);

        bool appliedUpdate = false;

        foreach (var (steamId, blob) in updates)
        {
            _playerTokens[steamId] = blob;
            appliedUpdate = true;
        }

        if (appliedUpdate)
        {
            SaveTokens();
        }
    }
    static void SaveTokens()
    {
        File.WriteAllText(TokensPath,
            JsonSerializer.Serialize(_playerTokens, _jsonOptions));
    }
}