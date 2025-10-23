using HarmonyLib;
using Penumbra.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Network;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using static Penumbra.Services.PlayerService;
using static Penumbra.VExtensions;
using User = ProjectM.Network.User;

namespace Penumbra.Patches;

[HarmonyPatch]
internal static class ServerBootstrapSystemPatches
{
    static readonly WaitForSeconds _delay = new(2.5f);

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    [HarmonyPostfix]
    static void OnUserConnectedPostfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out int userIndex)) return;
        ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];

        Entity userEntity = serverClient.UserEntity;
        User user = userEntity.GetUser();
        ulong steamId = user.PlatformId;
        
        Entity playerCharacter = user.LocalCharacter.GetEntityOnServer();
        bool exists = playerCharacter.Exists();

        if (exists)
        {
            PlayerInfo playerInfo = new()
            {
                CharEntity = playerCharacter,
                UserEntity = userEntity,
                User = user
            };

            HandleConnection(steamId, playerInfo);
        }
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
    [HarmonyPrefix]
    static void OnUserDisconnectedPrefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out var userIndex)) return;

        var user = __instance._ApprovedUsersLookup[userIndex].UserEntity.GetUser();
        ulong steamId = user.PlatformId;

        if (TokenService.PlayerTokens.TryGetValue(steamId, out var tokenData))
        {
            tokenData = TokenService.AccumulateTime(tokenData);
            steamId.UpdateAndSaveTokens(tokenData);
        }

        HandleDisconnection(steamId);
    }

    [HarmonyPatch(typeof(HandleCreateCharacterEventSystem), nameof(HandleCreateCharacterEventSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnCharacterCreated(HandleCreateCharacterEventSystem __instance)
    {
        using NativeAccessor<FromCharacter> fromCharacterEvents = __instance._CreateCharacterEventQuery.ToComponentDataArrayAccessor<FromCharacter>();

        try
        {
            for (int i = 0; i < fromCharacterEvents.Length; i++)
            {
                FromCharacter fromCharacter = fromCharacterEvents[i];
                Entity userEntity = fromCharacter.User;

                HandleCharacterCreatedRoutine(userEntity).Start();
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Error in HandleCreateCharacterEventSystem: {ex}");
        }
    }
    static IEnumerator HandleCharacterCreatedRoutine(Entity userEntity)
    {
        yield return _delay;

        User user = userEntity.GetUser();
        Entity playerCharacter = user.LocalCharacter.GetEntityOnServer();

        PlayerInfo playerInfo = new()
        {
            CharEntity = playerCharacter,
            UserEntity = userEntity,
            User = user
        };

        HandleConnection(user.PlatformId, playerInfo);
    }
}
