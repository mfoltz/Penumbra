using Il2CppInterop.Runtime;
using ProjectM;
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
                if (entity.Has<Trader>() && entity.Read<UnitStats>().FireResistance._Value.Equals(10000))
                {
                    Trader trader = entity.Read<Trader>();
                    if (trader.RestockTime >= 1 && trader.RestockTime <= 5) // check for mod merchants
                    {
                        int merchantConfig = (int)trader.RestockTime;
                        List<int> restockAmounts = Core.MerchantMap[merchantConfig][4];
                        var entryBuffer = entity.ReadBuffer<TraderEntry>();
                        for (int i = 0; i < restockAmounts.Count; i++)
                        {
                            var item = entryBuffer[i];
                            item.StockAmount = (ushort)restockAmounts[i];
                            entryBuffer[i] = item;
                        }
                    }
                }
            }
            yield return wait;
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
