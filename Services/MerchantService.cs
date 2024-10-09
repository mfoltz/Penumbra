using Il2CppInterop.Runtime;
using ProjectM;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Merchants.Services;
internal class MerchantService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly int RestockMinutes = Plugin.RestockTime;

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
        //Core.StartCoroutine(Debug());
    }
    static IEnumerator RestockTrader()
    {
        WaitForSeconds wait = new(60 * RestockMinutes);
        WaitForSeconds startDelay = new(60);
        while (true)
        {
            yield return startDelay;

            IEnumerable<Entity> traders = GetTradersEnumerable();
            foreach (Entity entity in traders)
            {
                if (entity.Has<Trader>() && entity.Read<UnitStats>().FireResistance._Value.Equals(10000)) // check for mod merchants
                {
                    Trader trader = entity.Read<Trader>();
                    if (trader.RestockTime >= 1 && trader.RestockTime <= 7) // double-check for mod merchants
                    {
                        int merchantConfig = (int)trader.RestockTime;
                        List<int> restockAmounts = Core.MerchantMap[merchantConfig][4];
                        var entryBuffer = entity.ReadBuffer<TraderEntry>();

                        if (entryBuffer.Length != restockAmounts.Count) // update inventory
                        {
                            UpdateMerchantInventory(entity, merchantConfig);
                        }
                        else // restock inventory
                        {
                            for (int i = 0; i < restockAmounts.Count; i++)
                            {
                                var item = entryBuffer[i];
                                item.StockAmount = (ushort)restockAmounts[i];
                                entryBuffer[i] = item;
                            }
                        }
                    }
                }
            }

            yield return wait;
        }
    }
    static void UpdateMerchantInventory(Entity merchant, int merchantConfig)
    {
        List<List<int>> merchantConfigs = Core.MerchantMap[merchantConfig];

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
                CostStartIndex = (byte)(i),
                FullRechargeTime = 60,
                OutputCount = 1,
                OutputStartIndex = (byte)(i),
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
