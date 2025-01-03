using Il2CppInterop.Runtime;
using ProjectM;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Penumbra.Service;
internal class MerchantService
{
    static EntityManager EntityManager => Core.EntityManager;
    
    static readonly WaitForSeconds Delay = new(150);

    static readonly Dictionary<Entity, DateTime> NextRestockTimes = [];
    static readonly Dictionary<int, int> MerchantRestockTimes = Core.ParseConfigString(Plugin.MerchantRestockTimes)
        .Select((value, index) => new { Key = index + 1, Value = value })
        .ToDictionary(pair => pair.Key, pair => pair.Value);

    static readonly ComponentType[] TraderComponent =
        [
            ComponentType.ReadOnly(Il2CppType.Of<Trader>()),
        ];

    static EntityQuery TraderQuery;
    public MerchantService()
    {
        TraderQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = TraderComponent,
            Options = EntityQueryOptions.IncludeDisabled
        });

        Core.StartCoroutine(RestockTrader());
    }
    static IEnumerator RestockTrader()
    {
        while (true)
        {
            IEnumerable<Entity> traders = GetTradersEnumerable();

            foreach (Entity entity in traders)
            {
                if (entity.Has<Trader>() && entity.Read<UnitStats>().FireResistance._Value.Equals(10000)) // check for mod merchants
                {
                    Trader trader = entity.Read<Trader>();

                    if (trader.RestockTime >= 1 && trader.RestockTime <= 5) // double-check for mod merchants
                    {
                        int merchantConfig = (int)trader.RestockTime;

                        // Initialize the next restock time for the merchant if not already set
                        if (!NextRestockTimes.ContainsKey(entity))
                        {
                            NextRestockTimes[entity] = DateTime.UtcNow.AddMinutes(MerchantRestockTimes[merchantConfig]);
                            //Core.Log.LogInfo($"Initialized restock time for merchant {entity} to {NextRestockTimes[entity]}!");
                        }

                        // Check if the current time has passed the next restock time
                        if (DateTime.UtcNow >= NextRestockTimes[entity])
                        {
                            //Core.Log.LogInfo($"Restocking merchant {entity} ({DateTime.UtcNow}|{NextRestockTimes[entity]})");
                            List<int> restockAmounts = Core.MerchantStockMap[merchantConfig][4];
                            var entryBuffer = entity.ReadBuffer<TraderEntry>();

                            if (entryBuffer.Length != restockAmounts.Count) // Update inventory
                            {
                                UpdateMerchantInventory(entity, merchantConfig);
                            }
                            else // Restock inventory
                            {
                                for (int i = 0; i < restockAmounts.Count; i++)
                                {
                                    var item = entryBuffer[i];
                                    item.StockAmount = (ushort)restockAmounts[i];
                                    entryBuffer[i] = item;
                                }
                            }

                            // Update the next restock time
                            NextRestockTimes[entity] = DateTime.UtcNow.AddMinutes(MerchantRestockTimes[merchantConfig]);
                        }
                    }
                }
            }

            yield return Delay;
        }
    }
    static void UpdateMerchantInventory(Entity merchant, int merchantConfig)
    {
        List<List<int>> merchantConfigs = Core.MerchantStockMap[merchantConfig];

        List<PrefabGUID> outputItems = merchantConfigs[0].Select(x => new PrefabGUID(x)).ToList();
        List<int> outputAmounts = merchantConfigs[1];

        List<PrefabGUID> inputItems = merchantConfigs[2].Select(x => new PrefabGUID(x)).ToList();
        List<int> inputAmounts = merchantConfigs[3];

        List<int> stockAmounts = merchantConfigs[4];

        int length = outputItems.Count;
        if (!outputAmounts.Count.Equals(length) || !inputItems.Count.Equals(length) || !inputAmounts.Count.Equals(length) || !stockAmounts.Count.Equals(length))
        {
            Core.Log.LogInfo($"Invalid merchant config for {merchantConfig}: {outputItems.Count}, {outputAmounts.Count}, {inputItems.Count}, {inputAmounts.Count}, {stockAmounts.Count}");
            return;
        }

        var outputBuffer = merchant.ReadBuffer<TradeOutput>();
        var entryBuffer = merchant.ReadBuffer<TraderEntry>();
        var inputBuffer = merchant.ReadBuffer<TradeCost>();

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
                CostStartIndex = (byte)i,
                FullRechargeTime = 60,
                OutputCount = 1,
                OutputStartIndex = (byte)i,
                StockAmount = (ushort)stockAmounts[i]
            });
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
        userEntities = TraderQuery.ToEntityArray(allocator);
        return default;
    }
}
