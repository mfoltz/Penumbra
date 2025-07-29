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
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;

    [Command(name: "spawnmerchant", shortHand: "sm", adminOnly: true, usage: ".pen sm [TraderPrefab] [Wares]", description: "Spawns merchant at mouse location with configured wares ('.pen sm 1631713257 3' will spawn a major noctem trader with the third wares as configured).")]
    public static void SpawnMerchantCommand(ChatCommandContext ctx, int trader, int wares)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        float3 aimPosition = entityInput.AimPosition;
        PrefabGUID merchantPrefabGuid = new(trader);

        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(merchantPrefabGuid, out Entity prefab) || !prefab.IsTrader())
        {
            ctx.Reply("Invalid trader prefabGuid!");
            return;
        }

        if (wares < 1 || wares > Plugin.Merchants.Count)
        {
            ctx.Reply($"Wares input out of range! (<color=white>{1}</color>-<color=white>{Plugin.Merchants.Count}</color>)");
            return;
        }

        int index = wares - 1;
        MerchantWares merchantWares = GetMerchantWares(index);
        SpawnMerchant(merchantPrefabGuid, entityInput.AimPosition, merchantWares);
        ctx.Reply($"Spawned merchant: <color=white>{merchantPrefabGuid.GetPrefabName()}</color> " +
            $"[<color=yellow>{(int)aimPosition.x}, {(int)aimPosition.y}, {(int)aimPosition.z}</color>] " +
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
}