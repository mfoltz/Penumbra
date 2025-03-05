using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Penumbra.Service.MerchantService;

namespace Penumbra.Commands;

[CommandGroup("penumbra")]
internal static class MerchantCommands
{
    static readonly PrefabGUID _noctemMajorTrader = new(1631713257);
    static readonly PrefabGUID _noctemMinorTrader = new(345283594);

    [Command(name: "spawnmerchant", shortHand: "spawn", adminOnly: true, usage: ".penumbra spawn [major/minor]", description: "Spawns CHAR_Trader_Noctem_Major PrefabGuid(1631713257) at mouse location.")]
    public static void SpawnMerchantCommand(ChatCommandContext ctx, string trader = "minor")
    {
        EntityCommandBuffer entityCommandBuffer = Core.EntityCommandBufferSystem.CreateCommandBuffer();
        DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;

        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;
        User user = ctx.Event.User;
        int index = user.Index;

        FromCharacter fromCharacter = new() { Character = character, User = userEntity };
        EntityInput entityInput = character.Read<EntityInput>();

        PrefabGUID merchantPrefabGuid;

        if (trader.Equals("major"))
        {
            merchantPrefabGuid = _noctemMajorTrader;
        }
        else if (trader.Equals("minor"))
        {
            merchantPrefabGuid = _noctemMinorTrader;
        }
        else
        {
            ctx.Reply("Invalid merchant type! Must be <color=white>'major'</color> or <color=white>'minor'</color> (Noctem traders are the only options currently for simplicity and ease of use)");
            return;
        }

        SpawnDebugEvent debugEvent = new()
        {
            PrefabGuid = merchantPrefabGuid,
            Control = false,
            Roam = true,
            Team = SpawnDebugEvent.TeamEnum.Neutral,
            Level = 100,
            Position = entityInput.AimPosition,
            DyeIndex = 0
        };

        debugEventsSystem.SpawnDebugEvent(index, ref debugEvent, entityCommandBuffer, ref fromCharacter);

        ctx.Reply($"Spawned Penumbra merchant (<color=white>{merchantPrefabGuid.GetPrefabName()}</color>) at mouse position!");
    }

    [Command(name: "changewares", shortHand: "wares", adminOnly: true, usage: ".penumbra wares [#]", description: "Applies merchant wares configuration to valid hovered merchant.")]
    public static void ApplyMerchantCommand(ChatCommandContext ctx, int merchant)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        if (merchant < 1 || merchant > Plugin.Merchants.Count)
        {
            ctx.Reply($"Merchant wares # must be between <color=white>{1}</color> and <color=white>{Plugin.Merchants.Count}</color>!");
            return;
        }

        MerchantWares merchantWares = GetMerchantWares(merchant - 1);

        if (entityInput.HoveredEntity.Read<UnitStats>().FireResistance._Value.Equals(10000) && (entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMajorTrader) || entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMinorTrader)))
        {
            var outputBuffer = entityInput.HoveredEntity.ReadBuffer<TradeOutput>();
            var entryBuffer = entityInput.HoveredEntity.ReadBuffer<TraderEntry>();
            var inputBuffer = entityInput.HoveredEntity.ReadBuffer<TradeCost>();

            outputBuffer.Clear();
            entryBuffer.Clear();
            inputBuffer.Clear();

            for (int i = 0; i < merchantWares.OutputItems.Count; i++)
            {
                outputBuffer.Add(new TradeOutput
                {
                    Amount = (ushort)merchantWares.OutputAmounts[i],
                    Item = merchantWares.OutputItems[i]
                });

                inputBuffer.Add(new TradeCost
                {
                    Amount = (ushort)merchantWares.InputAmounts[i],
                    Item = merchantWares.InputItems[i]
                });

                entryBuffer.Add(new TraderEntry
                {
                    RechargeInterval = 10,
                    CostCount = 1,
                    CostStartIndex = (byte)(i),
                    FullRechargeTime = 60,
                    OutputCount = 1,
                    OutputStartIndex = (byte)(i),
                    StockAmount = (ushort)merchantWares.StockAmounts[i]
                });
            }

            entityInput.HoveredEntity.Write(new Trader { RestockTime = merchant, NextRestockTime = 0, PrevRestockTime = 0 });

            ctx.Reply($"Wares (<color=white>{merchant}</color>) updated!");
        }
        else
        {
            ctx.Reply($"Not hovering over Penumbra merchant!");
        }
    }

    [Command(name: "merchantremove", shortHand: "remove", adminOnly: true, usage: ".penumbra remove", description: "Removes hovered Penumbra merchant.")]
    public static void RemoveMerchantCommand(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        if (entityInput.HoveredEntity.Read<UnitStats>().FireResistance._Value.Equals(10000) && (entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMajorTrader) || entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMinorTrader)))
        {
            DestroyUtility.Destroy(Core.EntityManager, entityInput.HoveredEntity);
        }
        else
        {
            ctx.Reply($"Not hovering over Penumbra merchant!");
        }
    }
}