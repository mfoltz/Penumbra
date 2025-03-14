using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Penumbra.Plugin;

namespace Penumbra.Service;
internal class MerchantService
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;

    static readonly WaitForSeconds _spawnDelay = new(0.25f);
    static readonly WaitForSeconds _startDelay = new(60f);
    static readonly WaitForSeconds _delay = new(300f);

    static readonly PrefabGUID _noctemMajorTrader = new(1631713257);
    static readonly PrefabGUID _noctemMinorTrader = new(345283594);
    static readonly PrefabGUID _defaultEmoteBuff = new(-988102043);

    static readonly Dictionary<Entity, DateTime> _nextRestockTimes = [];
    static readonly List<Entity> _penumbraTraders = [];

    static readonly ComponentType[] _traderComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Trader>()),
        ComponentType.ReadOnly(Il2CppType.Of<Immortal>()),
    ];

    static EntityQuery _traderQuery;
    public class MerchantWares
    {
        public List<PrefabGUID> OutputItems;
        public List<int> OutputAmounts;
        public List<PrefabGUID> InputItems;
        public List<int> InputAmounts;
        public List<int> StockAmounts;
        public int RestockTime;
        public int MerchantIndex;
    }

    static readonly List<MerchantWares> _merchants = [];
    public static MerchantWares GetMerchantWares(int index) => _merchants[index];
    public MerchantService()
    {
        _traderQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _traderComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });

        PopulateMerchantWares();
        RestockTraders().Start();
    }
    static IEnumerator RestockTraders()
    {
        yield return _startDelay;

        while (true)
        {
            IEnumerable<Entity> traders = GetTradersEnumerable();

            foreach (Entity entity in traders)
            {
                if (entity.Has<Trader>() && entity.Read<UnitStats>().FireResistance._Value.Equals(10000)) // check for mod merchants
                {
                    // Core.Log.LogWarning($"Handling Penumbra merchant in restock loop...");
                    // Trader trader = entity.Read<Trader>();
                    float holyResistance = entity.Read<UnitStats>().HolyResistance._Value;

                    if (holyResistance >= 1 && holyResistance <= Merchants.Count) // double-check for mod merchants
                    {
                        // Core.Log.LogWarning($"Retrieving wares...");
                        // int merchant = (int)trader.RestockTime;

                        int merchant = (int)holyResistance;
                        MerchantWares merchantWares = GetMerchantWares(merchant - 1);

                        // Initialize the next restock time for the merchant if not already set
                        if (!_nextRestockTimes.ContainsKey(entity))
                        {
                            _nextRestockTimes[entity] = DateTime.UtcNow.AddMinutes(merchantWares.RestockTime);
                            //Core.Log.LogInfo($"Initialized restock time for merchant {entity} to {NextRestockTimes[entity]}!");
                        }

                        // Core.Log.LogWarning($"Checking next restock time - ({merchant})");
                        // Check if the current time has passed the next restock time
                        if (DateTime.UtcNow >= _nextRestockTimes[entity])
                        {
                            //Core.Log.LogInfo($"Restocking merchant {entity} ({DateTime.UtcNow}|{NextRestockTimes[entity]})");
                            List<int> restockAmounts = merchantWares.StockAmounts;
                            var entryBuffer = entity.ReadBuffer<TraderEntry>();

                            if (entryBuffer.Length != restockAmounts.Count) // Update inventory
                            {
                                // Core.Log.LogWarning($"Updating merchant inventory...");
                                UpdateMerchantInventory(entity, merchantWares);
                            }
                            else // Restock inventory
                            {
                                // Core.Log.LogWarning($"Restocking merchant inventory...");
                                for (int i = 0; i < restockAmounts.Count; i++)
                                {
                                    var item = entryBuffer[i];
                                    item.StockAmount = (ushort)restockAmounts[i];
                                    entryBuffer[i] = item;
                                }
                            }

                            // Update the next restock time
                            _nextRestockTimes[entity] = DateTime.UtcNow.AddMinutes(merchantWares.RestockTime);
                        }
                    }
                }
            }

            yield return _delay;
        }
    }
    static void PopulateMerchantWares()
    {
        foreach (MerchantConfig merchantConfig in Merchants)
        {
            MerchantWares merchantWares = new()
            {
                OutputItems = [..merchantConfig.OutputItems.Select(id => new PrefabGUID(int.Parse(id)))],
                OutputAmounts = [..merchantConfig.OutputAmounts],
                InputItems = [..merchantConfig.InputItems.Select(id => new PrefabGUID(int.Parse(id)))],
                InputAmounts = [..merchantConfig.InputAmounts],
                StockAmounts = [..merchantConfig.StockAmounts],
                RestockTime = merchantConfig.RestockTime,
                MerchantIndex = Merchants.IndexOf(merchantConfig)
            };

            _merchants.Add(merchantWares);
        }
    }
    public static void SpawnTrader(PrefabGUID traderPrefabGuid, float3 aimPosition, float direction, MerchantWares wares)
    {
        Entity trader = ServerGameManager.InstantiateEntityImmediate(Entity.Null, traderPrefabGuid);
        ModifyTrader(trader, aimPosition, direction).Start();
    }
    static IEnumerator ModifyTrader(Entity trader, float3 aimPosition, float direction)
    {
        yield return _spawnDelay;
        
        if (!)
    }
    public static void ModifyDefaultEmoteBuff(Entity buffEntity, Entity merchant)
    {
        buffEntity.With((ref ModifyRotation modifyRotation) =>
        {
            modifyRotation.TargetDirectionType = TargetDirectionType.AimDirection;
            modifyRotation.SnapToDirection = true;
            modifyRotation.Type = RotationModificationType.Set;
        });
    }
    static void UpdateMerchantInventory(Entity merchant, MerchantWares merchantWares)
    {
        int length = merchantWares.OutputItems.Count;

        if (!merchantWares.OutputAmounts.Count.Equals(length) || !merchantWares.InputItems.Count.Equals(length) 
            || !merchantWares.InputAmounts.Count.Equals(length) || !merchantWares.StockAmounts.Count.Equals(length))
        {
            Core.Log.LogWarning($"Merchant data length mismatch - ({merchantWares.MerchantIndex + 1})");
            return;
        }

        var outputBuffer = merchant.ReadBuffer<TradeOutput>();
        var entryBuffer = merchant.ReadBuffer<TraderEntry>();
        var inputBuffer = merchant.ReadBuffer<TradeCost>();

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
                CostStartIndex = (byte)i,
                FullRechargeTime = 60,
                OutputCount = 1,
                OutputStartIndex = (byte)i,
                StockAmount = (ushort)merchantWares.StockAmounts[i]
            });
        }
    }
    static IEnumerable<Entity> GetPenumbraTraders()
    {
        JobHandle handle = GetTraders(out NativeArray<Entity> traderEntities, Allocator.Temp);
        handle.Complete();
        
        try
        {
            foreach (Entity entity in traderEntities)
            {
                if (EntityManager.Exists(entity))
                {
                    yield return entity;
                }
            }
        }
        finally
        {
            traderEntities.Dispose();
        }
    }
    static IEnumerable<Entity> GetTradersEnumerable()
    {
        JobHandle handle = GetTraders(out NativeArray<Entity> traderEntities, Allocator.TempJob);
        handle.Complete();

        try
        {
            foreach (Entity entity in traderEntities)
            {
                if (EntityManager.Exists(entity))
                {
                    yield return entity;
                }
            }
        }
        finally
        {
            traderEntities.Dispose();
        }
    }
    static JobHandle GetTraders(out NativeArray<Entity> userEntities, Allocator allocator = Allocator.TempJob)
    {
        userEntities = _traderQuery.ToEntityArray(allocator);
        return default;
    }
}
