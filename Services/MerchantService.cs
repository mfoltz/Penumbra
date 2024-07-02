using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Merchants.Services;
internal class MerchantService
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static readonly int RestockMinutes = Plugin.RestockTime;
    static readonly PrefabGUID invulnerable = new(1811209060);

    static readonly ComponentType[] TraderComponent =
        [
            ComponentType.ReadOnly(Il2CppType.Of<Trader>()),
        ];

    static EntityQuery TraderQuery;
    public MerchantService()
    {
        TraderQuery = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc
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
            yield return new WaitForSeconds(60 * RestockMinutes);
            NativeArray<Entity> traders = TraderQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in traders)
                {
                    if (entity.Has<Trader>() && ServerGameManager.HasBuff(entity, invulnerable.ToIdentifier()))
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
            }
            finally
            {
                traders.Dispose();
            }
        }
    }
}
