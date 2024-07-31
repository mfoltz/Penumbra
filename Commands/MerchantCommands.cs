using BepInEx;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using VampireCommandFramework;

namespace Merchants.Commands;
internal static class MerchantCommands
{
    [Command(name: "spawnMerchant", shortHand: "merchant", adminOnly: true, usage: ".merchant [TraderPrefab]", description: "Spawns trader prefab at mouse location.")]
    public static void SpawnMerchantCommand(ChatCommandContext ctx, int trader)
    {
        EntityCommandBuffer entityCommandBuffer = Core.EntityCommandBufferSystem.CreateCommandBuffer();
        DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;

        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;
        User user = ctx.Event.User;
        int index = user.Index;

        PrefabGUID traderPrefab = new(trader);

        if (!Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(traderPrefab, out Entity traderEntity) || !traderEntity.Read<PrefabGUID>().LookupName().Contains("CHAR_Trader"))
        {
            Core.Log.LogInfo($"Couldn't find matching trader from prefab.");
            return;
        }

        FromCharacter fromCharacter = new() { Character = character, User = userEntity };
        EntityInput entityInput = character.Read<EntityInput>();

        SpawnDebugEvent debugEvent = new()
        {
            PrefabGuid = traderPrefab,
            Control = false,
            Roam = false,
            Team = SpawnDebugEvent.TeamEnum.Neutral,
            Level = 100,
            Position = entityInput.AimPosition,
            DyeIndex = 0
        };

        debugEventsSystem.SpawnDebugEvent(index, ref debugEvent, entityCommandBuffer, ref fromCharacter);
    }

    [Command(name: "applyMerchant", shortHand: "apply", adminOnly: true, usage: ".apply [#]", description: "Applies merchant configuration to hovered merchant.")]
    public static void ApplyMerchantCommand(ChatCommandContext ctx, int merchantConfig)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        if (merchantConfig < 1 || merchantConfig > 7)
        {
            Core.Log.LogInfo($"Merchant configuration must be between 1 and 5.");
            return;
        }

        List<List<int>> merchantConfigs = Core.MerchantMap[merchantConfig];
        
        List<PrefabGUID> outputItems = merchantConfigs[0].Select(x => new PrefabGUID(x)).ToList();
        List<int> outputAmounts = merchantConfigs[1];

        List<PrefabGUID> inputItems = merchantConfigs[2].Select(x => new PrefabGUID(x)).ToList();
        List<int> inputAmounts = merchantConfigs[3];

        List<int> stockAmounts = merchantConfigs[4];

        if (entityInput.HoveredEntity.Read<UnitStats>().FireResistance._Value.Equals(10000) && entityInput.HoveredEntity.Read<PrefabGUID>().LookupName().Contains("CHAR_Trader"))
        {
            var outputBuffer = entityInput.HoveredEntity.ReadBuffer<TradeOutput>();
            var entryBuffer = entityInput.HoveredEntity.ReadBuffer<TraderEntry>();
            var inputBuffer = entityInput.HoveredEntity.ReadBuffer<TradeCost>();

            outputBuffer.Clear();
            entryBuffer.Clear();
            inputBuffer.Clear();

            for (int i = 0; i < outputItems.Count; i++)
            {
                outputBuffer.Add(new TradeOutput
                {
                    Amount = (ushort)outputAmounts[i],
                    Item = outputItems[i]
                });

                inputBuffer.Add(new TradeCost
                {
                    Amount = (ushort)inputAmounts[i],
                    Item = inputItems[i]
                });

                entryBuffer.Add(new TraderEntry
                {
                    RechargeInterval = 10,
                    CostCount = 1,
                    CostStartIndex = (byte)(i),
                    FullRechargeTime = 60,
                    OutputCount = 1,
                    OutputStartIndex = (byte)(i),
                    StockAmount = (ushort)stockAmounts[i]
                });
            }
            entityInput.HoveredEntity.Write(new Trader { RestockTime = merchantConfig, NextRestockTime = 0, PrevRestockTime = 0 });
        }
        else
        {
            Core.Log.LogInfo($"Hovered entity is not a valid merchant to configure.");
        }
    }

    [Command(name: "removeMerchant", shortHand: "remove", adminOnly: true, usage: ".remove", description: "Destroys hovered merchant.")]
    public static void RemoveMerchantCommand(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        if (entityInput.HoveredEntity.Read<UnitStats>().FireResistance._Value.Equals(10000) && entityInput.HoveredEntity.Read<PrefabGUID>().LookupName().Contains("CHAR_Trader"))
        {
            DestroyUtility.Destroy(Core.EntityManager, entityInput.HoveredEntity);
        }
        else
        {
            Core.Log.LogInfo($"Hovered entity is not a valid merchant to remove.");
        }
    }
}