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