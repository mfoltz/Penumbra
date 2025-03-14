using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
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
    static readonly PrefabGUID _noctemMajorTrader = new(1631713257);
    static readonly PrefabGUID _noctemMinorTrader = new(345283594);
    static readonly PrefabGUID _defaultEmoteBuff = new(-988102043);

    static readonly Dictionary<string, float> _directionToDegrees = new()
    {
        { "north", 0f },
        { "east", 90f },
        { "south", 180f },
        { "west", 270f }
    };

    [Command(name: "spawnmerchant", shortHand: "sm", adminOnly: true, usage: ".pen sm [TraderPrefabGuid] [Direction] [Wares]", description: "Spawns Noctem merchant (major or minor) at mouse location with entered direction (north|south|east|west) or roaming (true|false).")]
    public static void SpawnMerchantCommand(ChatCommandContext ctx, int trader, string direction, int wares)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;
        User user = ctx.Event.User;

        EntityInput entityInput = character.Read<EntityInput>();
        PrefabGUID merchantPrefabGuid = new(trader);

        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(merchantPrefabGuid, out Entity prefab) || !prefab.IsTrader())
        {
            ctx.Reply("Invalid trader prefabGuid!");
            return;
        }

        float rotation = 0f;
        var matchedDirection = _directionToDegrees.Keys.FirstOrDefault(direction => direction.StartsWith(direction, StringComparison.OrdinalIgnoreCase));

        if (matchedDirection != null)
        {
            rotation = _directionToDegrees[matchedDirection];
        }
        else if (_directionToDegrees.ContainsValue(float.TryParse(direction, out float parsedRotation) ? parsedRotation : -1))
        {
            rotation = parsedRotation;
        }
        else if (int.TryParse(direction, out int parsedDirection) && parsedDirection.Equals(-1f))
        {
            // Get angle of vector from aimPosition to playerPosition
            float3 position = character.GetPosition();
            float3 aimPosition = entityInput.AimPosition;
            float3 normalizedDirection = math.normalize(position - aimPosition);

            // Calculate the angle in degrees
            float angleRadians = math.atan2(normalizedDirection.x, normalizedDirection.z);
            rotation = math.degrees(angleRadians);
        }

        if (wares < 1 || wares > Plugin.Merchants.Count)
        {
            ctx.Reply($"Wares input out of range! (<color=white>{1}</color>-<color=white>{Plugin.Merchants.Count}</color>)");
            return;
        }

        MerchantWares merchantWares = GetMerchantWares(--wares);
        SpawnTrader(merchantPrefabGuid, entityInput.AimPosition, rotation, merchantWares);
        ctx.Reply($"Spawned merchant! (<color=green>{merchantPrefabGuid.GetPrefabName()}</color> " +
            $"| <color=white>{entityInput.AimPosition}</color> " +
            $"| <color=yellow>{rotation}°</color> " +
            $"| <color=magenta>{wares}</color>)");
    }

    [Command(name: "changewares", shortHand: "w", adminOnly: true, usage: ".pen w [#]", description: "Sets wares for hovered Penumbra merchant.")]
    public static void ApplyMerchantCommand(ChatCommandContext ctx, int merchant)
    {
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = playerCharacter.Read<EntityInput>();

        if (merchant < 1 || merchant > Plugin.Merchants.Count)
        {
            ctx.Reply($"Merchant wares # must be between <color=white>{1}</color> and <color=white>{Plugin.Merchants.Count}</color>!");
            return;
        }

        MerchantWares merchantWares = GetMerchantWares(merchant - 1);

        if (entityInput.HoveredEntity.Read<UnitStats>().FireResistance._Value.Equals(10000) && (entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMajorTrader) || entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMinorTrader)))
        {
            float restockTime = merchantWares.RestockTime * 60f;

            // Entity traderSyncEntity = CreateTraderSyncEntity();

            /*
            traderSyncEntity.With((ref Translation translation) =>
            {
                translation.Value = entityInput.HoveredEntity.GetPosition();
            });

            traderSyncEntity.With((ref LocalToWorld localToWorld) =>
            {
                localToWorld.Value = entityInput.HoveredEntity.Read<LocalToWorld>().Value;
            });

            traderSyncEntity.With((ref TraderSpawnData traderSpawnData) =>
            {
                traderSpawnData.RestockTime = restockTime;
                traderSpawnData.PrevRestockTime = Core.ServerTime;
                traderSpawnData.NextRestockTime = Core.ServerTime + (double)restockTime;
            });

            var unitCompositionBuffer = traderSyncEntity.ReadBuffer<UnitCompositionActiveUnit>();

            UnitCompositionActiveUnit unitCompositionActiveUnit = new()
            {
                UnitEntity = entityInput.HoveredEntity,
                // UnitPrefab = entityInput.HoveredEntity
            };

            unitCompositionBuffer.Add(unitCompositionActiveUnit);

            entityInput.HoveredEntity.AddWith((ref SpawnedBy spawnedBy) =>
            {
                spawnedBy.Value = traderSyncEntity;
            });
            */

            entityInput.HoveredEntity.With((ref Trader trader) =>
            {
                trader.RestockTime = restockTime;
                trader.PrevRestockTime = Core.ServerTime;
                trader.NextRestockTime = Core.ServerTime + (double)restockTime;
            });

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

                /*
                syncOutputBuffer.Add(new TradeOutput
                {
                    Amount = (ushort)merchantWares.OutputAmounts[i],
                    Item = merchantWares.OutputItems[i]
                });
                */

                inputBuffer.Add(new TradeCost
                {
                    Amount = (ushort)merchantWares.InputAmounts[i],
                    Item = merchantWares.InputItems[i]
                });

                /*
                syncInputBuffer.Add(new TradeCost
                {
                    Amount = (ushort)merchantWares.InputAmounts[i],
                    Item = merchantWares.InputItems[i]
                });
                */

                entryBuffer.Add(new TraderEntry
                {
                    RechargeInterval = restockTime,
                    CostCount = 1,
                    CostStartIndex = (byte)(i),
                    FullRechargeTime = restockTime,
                    OutputCount = 1,
                    OutputStartIndex = (byte)(i),
                    StockAmount = (ushort)merchantWares.StockAmounts[i]
                });

                /*
                syncEntryBuffer.Add(new TraderEntry
                {
                    RechargeInterval = restockTime,
                    CostCount = 1,
                    CostStartIndex = (byte)(i),
                    FullRechargeTime = restockTime,
                    OutputCount = 1,
                    OutputStartIndex = (byte)(i),
                    StockAmount = (ushort)merchantWares.StockAmounts[i]
                });
                */
            }

            entityInput.HoveredEntity.With((ref UnitStats unitStats) =>
            {
                unitStats.HolyResistance._Value = merchant;
            });

            if (entityInput.HoveredEntity.TryApplyAndGetBuff(_defaultEmoteBuff, out Entity buffEntity))
            {
                entityInput.HoveredEntity.With((ref EntityInput entityInput) =>
                {
                    entityInput.SetAllAimPositions(playerCharacter.GetPosition());
                });

                buffEntity.With((ref LifeTime lifetime) =>
                {
                    lifetime.Duration = 0f;
                    lifetime.EndAction = LifeTimeEndAction.None;
                });

                buffEntity.With((ref ModifyRotation modifyRotation) =>
                {
                    modifyRotation.TargetDirectionType = TargetDirectionType.SelfRotation;
                    modifyRotation.SnapToDirection = true;
                    modifyRotation.Type = RotationModificationType.Set;
                });
            }

            ctx.Reply($"Wares updated! (<color=white>{merchant}</color>)");
        }
        else
        {
            ctx.Reply($"Not hovering over Penumbra merchant!");
        }
    }

    [Command(name: "removemerchant", shortHand: "r", adminOnly: true, usage: ".pen r", description: "Removes hovered Penumbra merchant.")]
    public static void RemoveMerchantCommand(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        if (entityInput.HoveredEntity.Read<UnitStats>().FireResistance._Value.Equals(10000) && (entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMajorTrader) || entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMinorTrader)))
        {
            if (entityInput.HoveredEntity.TryGetComponent(out SpawnedBy spawnedBy))
            {
                if (spawnedBy.Value.Exists())
                {
                    spawnedBy.Value.Destroy();
                }
                else
                {
                    Core.Log.LogWarning("SpawnedBy entity does not exist!");
                }
            }
            else
            {
                Core.Log.LogWarning("Penumbra trader does not have SpawnedBy!");
            }

            DestroyUtility.Destroy(Core.EntityManager, entityInput.HoveredEntity);
        }
        else
        {
            ctx.Reply($"Not hovering over Penumbra merchant!");
        }
    }

    [Command(name: "test", shortHand: "t", adminOnly: true, usage: ".pen t [Value]", description: "Testing.")]
    public static void TestMerchantCommand(ChatCommandContext ctx, float value)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = character.Read<EntityInput>();

        if (entityInput.HoveredEntity.Read<UnitStats>().FireResistance._Value.Equals(10000) && (entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMajorTrader) || entityInput.HoveredEntity.Read<PrefabGUID>().Equals(_noctemMinorTrader)))
        {
            entityInput.HoveredEntity.With((ref Trader trader) =>
            {
                trader.RestockTime = value;
                trader.NextRestockTime = Core.ServerTime + (double)value;
                trader.PrevRestockTime = Core.ServerTime;
            });

            ctx.Reply($"Restock time set to <color=white>{value}</color>!");
        }
        else
        {
            ctx.Reply($"Not hovering over Penumbra merchant!");
        }
    }
}