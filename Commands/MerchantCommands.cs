using Penumbra.Resources;
using Penumbra.Services;
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

    [Command(name: "spawnmerchant", shortHand: "sm", adminOnly: true, usage: ".pen sm [#]", description: "Spawns merchant as configured at mouse; defaults to major noctem trader (can set per merchant in config).")]
    public static void SpawnMerchantCommand(ChatCommandContext ctx, int wares)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput input = character.Read<EntityInput>();
        float3 aimPosition = input.AimPosition;

        if (wares < 1 || wares > Plugin.Merchants.Count)
        {
            ctx.Reply($"Wares index out of range! (<color=white>{1}</color>-<color=white>{Plugin.Merchants.Count}</color>)");
            return;
        }

        MerchantWares merchantWares = GetMerchantWares(--wares);
        PrefabGUID merchantPrefabGuid = merchantWares.TraderPrefab.HasValue()
            ? merchantWares.TraderPrefab
            : PrefabGUIDs.CHAR_Trader_Noctem_Major;

        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(merchantPrefabGuid, out Entity prefab)
            || !prefab.IsTrader())
        {
            ctx.Reply("Invalid trader prefabGuid!");
            return;
        }

        SpawnMerchant(merchantPrefabGuid, input.AimPosition, merchantWares);
        ctx.Reply($"[<color=white>{merchantPrefabGuid.GetPrefabName(true)}</color>] " +
            $"[<color=yellow>{(int)aimPosition.x}, {(int)aimPosition.y}, {(int)aimPosition.z}</color>] " +
            $"[<color=#00FFFF>{merchantWares.Name}</color><color=#b0b>|</color><color=green>{++wares}</color>]");
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
        bool isMerchant = hoveredEntity.IsMerchant();

        if (!isMerchant)
        {
            ctx.Reply("Not hovering over Penumbra merchant!");
            return;
        }

        if (TryRemoveMerchant(hoveredEntity))
        {
            ctx.Reply("Merchant removed!");
        }
        else
        {
            ctx.Reply("Failed to remove merchant!");
        }
    }
}