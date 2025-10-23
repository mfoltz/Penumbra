using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using static Penumbra.Plugin;

namespace Penumbra.Services;
internal static class PlayerService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _tokens = _tokensConfig.TokenSystem;
    static readonly bool _daily = _tokensConfig.DailyLogin;
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdPlayerInfoCache => _steamIdPlayerInfoCache;
    static readonly ConcurrentDictionary<ulong, PlayerInfo> _steamIdPlayerInfoCache = [];
    public struct PlayerInfo(Entity userEntity = default, Entity charEntity = default, User user = default)
    {
        public User User { get; set; } = user;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharEntity { get; set; } = charEntity;
    }
    static PlayerService()
    {
        ComponentType[] _userAllComponents =
        [
            ComponentType.ReadOnly(Il2CppType.Of<User>())
        ];

        EntityQuery userQuery = EntityManager.BuildEntityQuery(_userAllComponents, options: EntityQueryOptions.IncludeDisabled);
        BuildPlayerInfoCache(userQuery);
    }
    static void BuildPlayerInfoCache(EntityQuery userQuery)
    {
        NativeArray<Entity> userEntities = userQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity userEntity in userEntities)
            {
                if (!userEntity.Exists()) continue;

                User user = userEntity.GetUser();
                Entity character = user.LocalCharacter.GetEntityOnServer();

                ulong steamId = user.PlatformId;
                string playerName = user.CharacterName.Value;

                _steamIdPlayerInfoCache[steamId] = new PlayerInfo(userEntity, character, user);
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[PlayerService] BuildPlayerInfoCache() - {ex}");
        }
        finally
        {
            userEntities.Dispose();
        }
    }

    /*
    public static void HandleConnection(ulong steamId, PlayerInfo playerInfo)
    {
        _steamIdPlayerInfoCache[steamId] = playerInfo;

        if (!TokenService.PlayerTokens.ContainsKey(steamId))
        {
            steamId.CreateTokens();
        }
    }
    */
    public static void HandleConnection(ulong steamId, PlayerInfo player)
    {
        _steamIdPlayerInfoCache[steamId] = player;

        if (!_tokens) return;

        if (!TokenService.PlayerTokens.TryGetValue(steamId, out var tokenData))
        {
            steamId.CreateTokens();
        }

        tokenData.TimeData = new TokenService.TimeBlob(DateTime.UtcNow, tokenData.TimeData.LoginTime);
        steamId.UpdateAndSaveTokens(tokenData);

        if (_daily) TokenService.TryGiveDaily(player.User.PlatformId, player.User, player.CharEntity);
    }
    public static void HandleDisconnection(ulong steamId)
    {
        _steamIdPlayerInfoCache.TryRemove(steamId, out _);
    }
    public static bool TryGetPlayerInfo(this ulong steamId, out PlayerInfo playerInfo)
    {
        if (SteamIdPlayerInfoCache.TryGetValue(steamId, out playerInfo)) return true;
        return false;
    }
    public static bool TryGetOnlinePlayerByName(string characterName, out ulong steamId, out PlayerInfo playerInfo)
    {
        steamId = default;
        playerInfo = default;

        if (string.IsNullOrWhiteSpace(characterName)) return false;

        foreach (KeyValuePair<ulong, PlayerInfo> entry in SteamIdPlayerInfoCache)
        {
            PlayerInfo info = entry.Value;
            if (info.User is not { IsConnected: true } user) continue;

            string cachedName = user.CharacterName.Value;
            if (string.Equals(cachedName, characterName, StringComparison.OrdinalIgnoreCase))
            {
                steamId = entry.Key;
                playerInfo = info;
                return true;
            }
        }

        return false;
    }
}
