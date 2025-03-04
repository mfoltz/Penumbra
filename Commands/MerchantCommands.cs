using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Penumbra.Commands;

[CommandGroup("penumbra")]
internal static class MerchantCommands
{
    static readonly PrefabGUID _noctemMajorTrader = new(1631713257);
    static readonly PrefabGUID _noctemMinorTrader = new(345283594);

    [Command(name: "spawnmerchant", shortHand: "spawn", adminOnly: true, usage: ".penumbra spawn", description: "Spawns CHAR_Trader_Noctem_Major PrefabGuid(1631713257) at mouse location.")]
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
            ctx.Reply("Invalid merchant type! Must be <color=white>'major'</color> or <color=white>'minor'</color> Noctem trader.");
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

        ctx.Reply("Spawned mod merchant at mouse position!");
    }

    [Command(name: "changewares", shortHand: "wares", adminOnly: true, usage: ".penumbra wares [#]", description: "Applies merchant wares configuration to valid hovered merchant.")]
    public static void ApplyMerchantCommand(ChatCommandContext ctx, int merchantConfig)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        if (merchantConfig < 1 || merchantConfig > 5)
        {
            ctx.Reply($"Merchant wares must be between <color=white>{1}</color> and <color=white>{5}</color>.");

            return;
        }

        List<List<int>> merchantConfigs = Core.MerchantStockMap[merchantConfig];
        
        List<PrefabGUID> outputItems = merchantConfigs[0].Select(x => new PrefabGUID(x)).ToList();
        List<int> outputAmounts = merchantConfigs[1];

        List<PrefabGUID> inputItems = merchantConfigs[2].Select(x => new PrefabGUID(x)).ToList();
        List<int> inputAmounts = merchantConfigs[3];

        List<int> stockAmounts = merchantConfigs[4];

        if (entityInput.HoveredEntity.Read<UnitStats>().FireResistance._Value.Equals(10000) && (entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMajorTrader) || entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMinorTrader)))
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

            ctx.Reply($"Wares (<color=white>{merchantConfig}</color>) updated for mod merchant!");
        }
        else
        {
            ctx.Reply($"Not hovering over a valid merchant!");
        }
    }

    [Command(name: "merchantremove", shortHand: "remove", adminOnly: true, usage: ".penumbra remove", description: "Removes valid hovered merchant.")]
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
            ctx.Reply($"Not hovering over a valid merchant!");
        }
    }

    /*
    [Command(name: "restorelink", shortHand: "link", adminOnly: true, usage: ".penumbra link [PrefabGUID]", description: "Restores link between attachedBuffer entry and abilityGroupSlotBuffer entry if severed.")]
    public static void RestoreLinkCommand(ChatCommandContext ctx, int abilityGroup)
    {
        PrefabGUID abilityGroupPrefabGUID = new(abilityGroup);
        Entity character = ctx.Event.SenderCharacterEntity;
        string playerName = ctx.User.CharacterName.Value;

        if (playerName != allowedName)
        {
            ctx.Reply($"Nope.");
            return;
        }

        if (!Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(abilityGroupPrefabGUID, out Entity abilityGroupPrefab) || !abilityGroupPrefab.Has<AbilityGroupInfo>())
        {
            ctx.Reply($"AbilityGroup prefabGUID not found or not a valid ability group!");
            return;
        }

        if (character.TryGetBuffer<AttachedBuffer>(out var attachedBuffer) && character.TryGetBuffer<AbilityGroupSlotBuffer>(out var abilityGroupSlotBuffer)) // get entity with entered PrefabGUID in attachedBuffer, change slotIndex on AbilityGroupState
        {                                                                                                                                                     // from that to match the abilityGroupSlot entity with same PrefabGUID and missing state entity
            for (int i = 0; i < attachedBuffer.Length; i++)
            {
                AttachedBuffer attachedBufferElement = attachedBuffer[i];

                if (attachedBufferElement.PrefabGuid == abilityGroupPrefabGUID)
                {
                    Entity attachedEntity = attachedBufferElement.Entity;
                    int slotIndex = attachedEntity.TryGetComponent(out AbilityGroupState abilityGroupState) ? abilityGroupState.SlotIndex : -1;

                    if (slotIndex != -1)
                    {
                        for (int j = 0; j < abilityGroupSlotBuffer.Length; j++)
                        {
                            AbilityGroupSlotBuffer abilityGroupSlotBufferElement = abilityGroupSlotBuffer[j];

                            if (abilityGroupSlotBufferElement.BaseAbilityGroupOnSlot == abilityGroupPrefabGUID && slotIndex != j)
                            {
                                Entity abilityGroupSlotEntity = abilityGroupSlotBufferElement.GroupSlotEntity.GetEntityOnServer();

                                if (abilityGroupSlotEntity.TryGetComponent(out AbilityGroupSlot abilityGroupSlot) && !abilityGroupSlot.StateEntity.GetSyncedEntityOrNull().Exists())
                                {
                                    abilityGroupSlot.StateEntity = NetworkedEntity.ServerEntity(attachedEntity);
                                    abilityGroupState.SlotIndex = abilityGroupSlot.SlotId;

                                    abilityGroupSlotEntity.Write(abilityGroupSlot);
                                    attachedEntity.Write(abilityGroupState);

                                    ctx.Reply($"Restored link for <color=white>{abilityGroupPrefabGUID.GetPrefabName()}</color>!");
                                    return;
                                }
                            }
                        }

                        ctx.Reply($"Couldn't find any matching abilityGroupSlot entities with missing abilityGroupStates!");
                    }
                    else
                    {
                        ctx.Reply($"Couldn't get slotIndex for attached entity!");
                    }
                }
            }

            ctx.Reply($"Couldn't find <color=white>{abilityGroupPrefabGUID.LookupName()}</color> in attachedBuffer!");
        }
        else
        {
            ctx.Reply($"Couldn't get attachedBuffer or abilityGroupSlotBuffer for character!");
        }
    }
    */
}