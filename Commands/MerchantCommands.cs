using Penumbra.Service;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using VampireCommandFramework;
using static Penumbra.Service.MerchantService;

namespace Penumbra.Commands;

[CommandGroup(name: "penumbra", "pen")]
internal static class MerchantCommands
{
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;

    [Command(name: "spawnmerchant", shortHand: "sm", adminOnly: true, usage: ".pen sm [TraderPrefab] [Wares]", description: "Spawns merchant at mouse location with configured wares.")]
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
        ctx.Reply($"Spawned merchant: <color=white>{merchantPrefabGuid.GetPrefabName()}</color> | " +
            $"[<color=yellow>{(int)aimPosition.x}, {(int)aimPosition.y}, {(int)aimPosition.z}</color>] " +
            $"(<color=#00FFFF>{wares}</color>)");
    }

    /*
    [Command(name: "patrolmerchant", shortHand: "pm", adminOnly: true, usage: ".pen pm", description: "Patrol merchant testing.")]
    public static void PatrolMerchantCommand(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        Entity hoveredEntity = entityInput.HoveredEntity;

        if (hoveredEntity.IsMerchant())
        {
            SpawnGlobalPatrol(hoveredEntity);
            ctx.Reply("Merchant patrolling!");
        }
        else
        {
            ctx.Reply("Not hovering over Penumbra merchant!");
        }
    }
    */

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