using System;
using System.Collections.Generic;
using System.Globalization;
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
    static readonly Random Random = new();
    static List<PrefabGUID>? traderPrefabs;
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;

    [Command(name: "spawnmerchant", shortHand: "sm", adminOnly: true,
        usage: ".pen sm [Wares] [TraderPrefab?]",
        description: "Spawns merchant at mouse location with configured wares (provide wares only to use a random trader or '.pen sm 1631713257 3' to spawn a major noctem trader with the third wares as configured).")]
    public static void SpawnMerchantCommand(ChatCommandContext ctx, int? traderPrefabId = null, int? wares = null)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        float3 aimPosition = entityInput.AimPosition;
        int? resolvedTraderPrefabId = traderPrefabId;

        if (wares is null && traderPrefabId is not null)
        {
            wares = traderPrefabId;
            resolvedTraderPrefabId = null;
        }

        if (wares is null)
        {
            ctx.Reply("Missing wares index!");
            return;
        }

        if (wares < 1 || wares > Plugin.Merchants.Count)
        {
            ctx.Reply($"Wares input out of range! (<color=white>{1}</color>-<color=white>{Plugin.Merchants.Count}</color>)");
            return;
        }

        PrefabGUID? merchantPrefabGuid = resolvedTraderPrefabId is null
            ? GetRandomTraderPrefabGuid()
            : new PrefabGUID(resolvedTraderPrefabId.Value);

        if (merchantPrefabGuid is null)
        {
            ctx.Reply("No trader prefabs available to spawn!");
            return;
        }

        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(merchantPrefabGuid.Value, out Entity prefab) || !prefab.IsTrader())
        {
            ctx.Reply("Invalid trader prefabGuid!");
            return;
        }

        int index = wares - 1;
        MerchantWares merchantWares = GetMerchantWares(index);
        SpawnMerchant(merchantPrefabGuid.Value, entityInput.AimPosition, merchantWares);
        ctx.Reply($"Spawned merchant: <color=white>{merchantPrefabGuid.Value.GetPrefabName()}</color> " +
            $"[<color=yellow>{((int)aimPosition.x).ToString(CultureInfo.InvariantCulture)}, {((int)aimPosition.y).ToString(CultureInfo.InvariantCulture)}, {((int)aimPosition.z).ToString(CultureInfo.InvariantCulture)}</color>] " +
            $"(<color=#00FFFF>{wares}</color>)");
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

    [Command(name: "additem", shortHand: "ai", adminOnly: true,
             usage: ".pen ai [Merchant] [Item] [Price] [Amount]",
             description: "Adds or updates merchant stock.")]
    public static void AddMerchantItemCommand(ChatCommandContext ctx, int merchant,
        int item, int price, int amount)
    {
        int index = merchant - 1;

        if (index < 0)
        {
            ctx.Reply("Invalid merchant index!");
            return;
        }

        PrefabGUID itemGuid = new(item);
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.ContainsKey(itemGuid))
        {
            ctx.Reply("Invalid item prefabGuid!");
            return;
        }

        if (price <= 0 || amount <= 0)
        {
            ctx.Reply("Price and amount must be positive numbers!");
            return;
        }

        AddMerchantItem(index, itemGuid, price, amount);
        ctx.Reply($"Updated merchant {merchant} with item {itemGuid.GetPrefabName()}.");
    }

    static PrefabGUID? GetRandomTraderPrefabGuid()
    {
        List<PrefabGUID>? cachedPrefabs = traderPrefabs;

        if (cachedPrefabs == null || cachedPrefabs.Count == 0)
        {
            cachedPrefabs = new List<PrefabGUID>();

            foreach (KeyValuePair<PrefabGUID, Entity> prefabPair in PrefabCollectionSystem._PrefabGuidToEntityMap)
            {
                if (prefabPair.Value.IsTrader())
                {
                    cachedPrefabs.Add(prefabPair.Key);
                }
            }

            traderPrefabs = cachedPrefabs;
        }

        if (cachedPrefabs.Count == 0)
        {
            return null;
        }

        int index = Random.Next(cachedPrefabs.Count);
        return cachedPrefabs[index];
    }
}