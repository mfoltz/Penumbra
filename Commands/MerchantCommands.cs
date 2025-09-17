using System;
using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using VampireCommandFramework;
using static Penumbra.Services.MerchantService;

namespace Penumbra.Commands;

[CommandGroup(name: "penumbra", "pen")]
internal static class MerchantCommands
{
    static readonly List<PrefabGUID> TraderPrefabs = [];
    static readonly Random Random = new();

    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;

    [Command(name: "spawnmerchant", shortHand: "sm", adminOnly: true, usage: ".pen sm [Wares] [TraderPrefab?]", description: "Spawns merchant at mouse location with configured wares ('.pen sm 3 1631713257' will spawn a major noctem trader with the third wares as configured).")]
    public static void SpawnMerchantCommand(ChatCommandContext ctx, int wares, int traderPrefabId = default)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        float3 aimPosition = entityInput.AimPosition;
        if (!TryResolveWares(ref wares, traderPrefabId, out MerchantWares merchantWares, out PrefabGUID legacyTraderPrefab))
        {
            ctx.Reply($"Wares input out of range! (<color=white>{1}</color>-<color=white>{Plugin.Merchants.Count}</color>)");
            return;
        }

        PrefabGUID merchantPrefabGuid;
        if (legacyTraderPrefab.Equals(default(PrefabGUID)))
        {
            if (traderPrefabId == default)
            {
                if (!TryGetRandomTraderPrefab(out merchantPrefabGuid))
                {
                    ctx.Reply("Unable to locate a trader prefab to spawn.");
                    return;
                }
            }
            else if (!TryGetTraderPrefab(traderPrefabId, out merchantPrefabGuid))
            {
                ctx.Reply("Invalid trader prefabGuid!");
                return;
            }
        }
        else
        {
            merchantPrefabGuid = legacyTraderPrefab;
        }

        SpawnMerchant(merchantPrefabGuid, entityInput.AimPosition, merchantWares);
        ctx.Reply($"Spawned merchant: <color=white>{merchantPrefabGuid.GetPrefabName()}</color> " +
            $"[<color=yellow>{(int)aimPosition.x}, {(int)aimPosition.y}, {(int)aimPosition.z}</color>] " +
            $"(<color=#00FFFF>{wares}</color>)");
    }

    static bool TryResolveWares(ref int wares, int traderPrefabId, out MerchantWares merchantWares, out PrefabGUID legacyTraderPrefab)
    {
        legacyTraderPrefab = default;

        if (IsValidWaresIndex(wares))
        {
            merchantWares = GetMerchantWares(wares - 1);
            return true;
        }

        if (traderPrefabId != default && IsValidWaresIndex(traderPrefabId) && TryGetTraderPrefab(wares, out legacyTraderPrefab))
        {
            wares = traderPrefabId;
            merchantWares = GetMerchantWares(wares - 1);
            return true;
        }

        merchantWares = default!;
        legacyTraderPrefab = default;
        return false;
    }

    static bool TryGetTraderPrefab(int prefabId, out PrefabGUID prefabGuid)
    {
        if (prefabId == default)
        {
            prefabGuid = default;
            return false;
        }

        prefabGuid = new(prefabId);

        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out Entity prefab) || !prefab.IsTrader())
        {
            prefabGuid = default;
            return false;
        }

        CacheTraderPrefab(prefabGuid);
        return true;
    }

    static bool TryGetRandomTraderPrefab(out PrefabGUID prefabGuid)
    {
        EnsureTraderPrefabsCached();

        if (TraderPrefabs.Count == 0)
        {
            prefabGuid = default;
            return false;
        }

        prefabGuid = TraderPrefabs[Random.Next(TraderPrefabs.Count)];
        return true;
    }

    static void EnsureTraderPrefabsCached()
    {
        if (TraderPrefabs.Count > 0)
        {
            return;
        }

        foreach (KeyValuePair<PrefabGUID, Entity> entry in PrefabCollectionSystem._PrefabGuidToEntityMap)
        {
            if (entry.Value.IsTrader())
            {
                CacheTraderPrefab(entry.Key);
            }
        }
    }

    static void CacheTraderPrefab(PrefabGUID prefabGuid)
    {
        if (!TraderPrefabs.Contains(prefabGuid))
        {
            TraderPrefabs.Add(prefabGuid);
        }
    }

    static bool IsValidWaresIndex(int wares)
    {
        return wares >= 1 && wares <= Plugin.Merchants.Count;
    }

    [Command(name: "removemerchant", shortHand: "rm", adminOnly: true, usage: ".pen rm", description: "Removes hovered merchant.")]
    public static void RemoveMerchantCommand(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        Entity hoveredEntity = entityInput.HoveredEntity;

        if (hoveredEntity.IsMerchant())
        {
            hoveredEntity.Destroy();
            ctx.Reply("Merchant removed!");
        }
        else
        {
            ctx.Reply("Not hovering over Penumbra merchant!");
        }
    }
}