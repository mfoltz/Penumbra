using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Penumbra.Plugin;

namespace Penumbra.Service;
internal class MerchantService
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static EntityTypeHandle EntityTypeHandle => EntityManager.GetEntityTypeHandle();
    static EntityStorageInfoLookup EntityStorageInfoLookup => EntityManager.GetEntityStorageInfoLookup();
    static ComponentTypeHandle<Energy> EnergyHandle => EntityManager.GetComponentTypeHandle<Energy>(true);

    const float TIME_CONSTANT = 60f;

    static readonly WaitForSeconds _spawnDelay = new(0.25f);
    static readonly WaitForSeconds _delay = new(TIME_CONSTANT);

    static readonly PrefabGUID _infiniteInvulnerabilityBuff = new(454502690);
    static readonly PrefabGUID _buffResistanceUberMob = Prefabs.BuffResistance_UberMobNoKnockback;

    static readonly ComponentType[] _merchantComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Trader>()),
        ComponentType.ReadOnly(Il2CppType.Of<Immortal>()),
        ComponentType.ReadOnly(Il2CppType.Of<Energy>())
    ];

    static readonly ComponentType[] _globalPatrolComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<GlobalPatrolState>()),
        ComponentType.ReadOnly(Il2CppType.Of<MovePatrolState>()),
        ComponentType.ReadOnly(Il2CppType.Of<VBloodUnitSpawnSource>())
    ];

    static EntityQuery _merchantQuery;

    // static EntityQuery _globalPatrolQuery;
    // static Entity _globalPatrol;
    public class MerchantWares
    {
        public List<PrefabGUID> OutputItems;
        public List<int> OutputAmounts;
        public List<PrefabGUID> InputItems;
        public List<int> InputAmounts;
        public List<int> StockAmounts;
        public int RestockInterval;
        public DateTime NextRestockTime = DateTime.MinValue;
        public int MerchantIndex;
        public bool Roam = false;
    }

    static readonly ConcurrentDictionary<Entity, MerchantWares> _activeMerchants = [];
    public static IReadOnlyDictionary<Entity, MerchantWares> ActiveMerchants => _activeMerchants;

    static readonly List<MerchantWares> _merchantWares = [];
    public static MerchantWares GetMerchantWares(int index) => _merchantWares[index];
    public MerchantService()
    {
        _merchantQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _merchantComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });

        /*
        _globalPatrolQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _globalPatrolComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });
        */

        try
        {
            PopulateMerchantWares();
            GetActiveMerchants();
            // GetGlobalPatrol();
            RestockMerchantsRoutine().Start();
        }
        catch (Exception ex)
        {
            Core.Log.LogError(ex);
        }
    }
    static IEnumerator RestockMerchantsRoutine()
    {
        while (true)
        {
            DateTime now = DateTime.UtcNow;

            foreach (var merchantWaresPair in ActiveMerchants)
            {
                try
                {
                    Entity merchant = merchantWaresPair.Key;
                    MerchantWares merchantWares = merchantWaresPair.Value;

                    if (merchantWares.NextRestockTime.Equals(DateTime.MinValue))
                    {
                        UpdateMerchantStock(merchant, merchantWares);
                        merchantWares.NextRestockTime = now.AddMinutes(merchantWares.RestockInterval);
                    }
                    else if (now >= merchantWares.NextRestockTime)
                    {
                        UpdateMerchantStock(merchant, merchantWares);
                        merchantWares.NextRestockTime = now.AddMinutes(merchantWares.RestockInterval);
                    }
                }
                catch (Exception ex)
                {
                    Core.Log.LogWarning($"RestockMerchantsRoutine() - {ex}");
                }

                yield return null;
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
                RestockInterval = merchantConfig.RestockTime,
                MerchantIndex = Merchants.IndexOf(merchantConfig)
            };

            _merchantWares.Add(merchantWares);
        }
    }
    public static void SpawnMerchant(PrefabGUID traderPrefabGuid, float3 aimPosition, MerchantWares wares, bool roam)
    {
        Entity trader = ServerGameManager.InstantiateEntityImmediate(Entity.Null, traderPrefabGuid);

        /*
        if (trader.Exists())
        {
            trader.AddWith((ref EntityOwner entityOwner) =>
            {
                entityOwner.Owner = trader;
            });

            trader.With((ref BuffResistances buffResistances) =>
            {
                buffResistances.InitialSettingGuid = _buffResistanceUberMob;
            });
            
            if (!roam && trader.TryApplyAndGetBuff(_infiniteInvulnerabilityBuff, out Entity buffEntity))
            {
                buffEntity.AddWith((ref ModifyMovementSpeedBuff modifyMovementSpeed) =>
                {
                    modifyMovementSpeed.MoveSpeed = 0;
                    modifyMovementSpeed.MultiplyAdd = false;
                });
            }
            else
            {
                trader.TryApplyBuff(_infiniteInvulnerabilityBuff);
            }
        }
        */

        ApplyOrRefreshModifications(trader, roam);
        ModifyMerchant(trader, aimPosition, wares).Start();
    }
    static void ApplyOrRefreshModifications(Entity merchant, bool roam)
    {
        if (merchant.Exists())
        {
            merchant.AddWith((ref EntityOwner entityOwner) =>
            {
                entityOwner.Owner = merchant;
            });

            merchant.AddWith((ref BuffResistances buffResistances) =>
            {
                buffResistances.InitialSettingGuid = _buffResistanceUberMob;
            });

            merchant.AddWith((ref Immortal immortal) =>
            {
                immortal.IsImmortal = true;
            });

            merchant.With((ref DynamicCollision dynamicCollision) =>
            {
                dynamicCollision.Immobile = true;
            });

            if (!roam && merchant.TryApplyAndGetBuff(_infiniteInvulnerabilityBuff, out Entity buffEntity))
            {
                buffEntity.AddWith((ref ModifyMovementSpeedBuff modifyMovementSpeed) =>
                {
                    modifyMovementSpeed.MoveSpeed = 0;
                    modifyMovementSpeed.MultiplyAdd = false;
                });
            }
            else
            {
                merchant.TryApplyBuff(_infiniteInvulnerabilityBuff);
            }
        }
    }

    /*
    public static void SpawnGlobalPatrol(Entity merchant)
    {
        if (_globalPatrol.Exists())
        {
            try
            {
                Entity globalPatrol = EntityManager.Instantiate(_globalPatrol);
                ModifyGlobalPatrol(globalPatrol, merchant);
            }
            catch (Exception ex)
            {
                Core.Log.LogError(ex);
            }
            finally
            {
                Core.Log.LogWarning("Global patrol entity spawned and linked to merchant!");
            }
        }
        else
        {
            Core.Log.LogWarning("Global patrol entity not found!");
        }
    }
    */
    static IEnumerator ModifyMerchant(Entity trader, float3 aimPosition, MerchantWares wares)
    {
        yield return _spawnDelay;

        trader.SetPosition(aimPosition);
        
        trader.With((ref UnitStats unitStats) =>
        {
            unitStats.DamageReduction._Value = 100f;
            unitStats.PhysicalResistance._Value = 100f;
            unitStats.SpellResistance._Value = 100f;
            unitStats.PvPProtected._Value = true;
            unitStats.PvPResilience._Value = 1;
            unitStats.HealthRecovery._Value = 1f;
        });

        trader.AddWith((ref Immortal immortal) =>
        {
            immortal.IsImmortal = true;
        });

        trader.With((ref DynamicCollision dynamicCollision) =>
        {
            dynamicCollision.Immobile = true;
        });

        trader.AddWith((ref Energy energy) =>
        {
            energy.MaxEnergy._Value = wares.MerchantIndex;
            energy.GainPerSecond._Value = wares.MerchantIndex;
            energy.RegainEnergyChance._Value = wares.MerchantIndex;
            energy.Value = wares.MerchantIndex;
        });

        UpdateMerchantStock(trader, wares);
        _activeMerchants.TryAdd(trader, wares);
    }

    /*
    static void ModifyGlobalPatrol(Entity globalPatrol, Entity merchant)
    {
        globalPatrol.TryRemove<UnitCompositionSpawner>();
        globalPatrol.TryRemove<VBloodUnitSpawnSource>();
        globalPatrol.TryRemove<UnitCompositionActiveUnit>();
        globalPatrol.TryRemove<FormationOffsetBuffer>();
        globalPatrol.TryRemove<UnitCompositionGroupEntry>();
        globalPatrol.TryRemove<UnitCompositionGroupUnitEntry>();

        globalPatrol.With((ref GlobalPatrolState globalPatrolState) =>
        {
            globalPatrolState.Direction = GlobalPatrolDirection.Forward;
            globalPatrolState.PatrolType = GlobalPatrolType.Roaming;
        });

        globalPatrol.With((ref Translation translation) =>
        {
            translation.Value = merchant.GetPosition();
        });

        if (globalPatrol.TryGetBuffer<FollowerBuffer>(out var buffer))
        {
            merchant.AddWith((ref Follower follower) =>
            {
                follower.Followed._Value = globalPatrol;
            });

            buffer.Clear();
            buffer.Add(new FollowerBuffer { Entity = NetworkedEntity.ServerEntity(merchant) });
        }
    }
    */
    static void UpdateMerchantStock(Entity merchant, MerchantWares merchantWares)
    {
        float restockTime = merchantWares.RestockInterval * TIME_CONSTANT;

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
                RechargeInterval = restockTime,
                CostCount = 1,
                CostStartIndex = (byte)i,
                FullRechargeTime = restockTime,
                OutputCount = 1,
                OutputStartIndex = (byte)i,
                StockAmount = (ushort)merchantWares.StockAmounts[i]
            });
        }
        
        merchant.With((ref Trader trader) =>
        {
            trader.RestockTime = restockTime;
            trader.PrevRestockTime = Core.ServerTime;
            trader.NextRestockTime = Core.ServerTime + (double)restockTime;
        });
    }
    static void GetActiveMerchants()
    {
        NativeArray<ArchetypeChunk> archetypeChunks = _merchantQuery.CreateArchetypeChunkArray(Allocator.Temp);
        int count = 0;

        try
        {
            foreach (ArchetypeChunk archetypeChunk in archetypeChunks)
            {
                NativeArray<Entity> entities = archetypeChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Energy> translations = archetypeChunk.GetNativeArray(EnergyHandle);

                for (int i = 0; i < archetypeChunk.Count; i++)
                {
                    Entity entity = entities[i];
                    if (!EntityStorageInfoLookup.Exists(entity)) continue;

                    int wares = entity.GetMerchantIndex();
                    if (wares < 0)
                    {
                        Core.Log.LogWarning($"Merchant entity has invalid wares index ({wares}), using default! (0)");
                        wares = 0;
                    }

                    MerchantWares merchantWares = GetMerchantWares(wares);

                    ApplyOrRefreshModifications(entity, merchantWares.Roam);
                    UpdateMerchantStock(entity, merchantWares);

                    _activeMerchants.TryAdd(entity, merchantWares);
                    count++;
                }
            }
        }
        finally
        {
            archetypeChunks.Dispose();
            Core.Log.LogWarning($"Tracking {count} Penumbra merchants!");
        }
    }

    /*
    static void GetGlobalPatrol()
    {
        NativeArray<ArchetypeChunk> archetypeChunks = _globalPatrolQuery.CreateArchetypeChunkArray(Allocator.Temp);
        int count = 0;

        try
        {
            foreach (ArchetypeChunk archetypeChunk in archetypeChunks)
            {
                NativeArray<Entity> entities = archetypeChunk.GetNativeArray(EntityTypeHandle);

                for (int i = 0; i < archetypeChunk.Count; i++)
                {
                    Entity entity = entities[i];
                    if (!EntityStorageInfoLookup.Exists(entity)) continue;
                    
                    if (entity.TryGetBuffer<UnitCompositionGroupUnitEntry>(out var buffer) && !buffer.IsEmpty)
                    {
                        UnitCompositionGroupUnitEntry unitCompositionGroupUnitEntry = buffer[0];
                        if (unitCompositionGroupUnitEntry.Unit.Equals(Prefabs.CHAR_VHunter_CastleMan)) _globalPatrol = entity;
                    }

                    // Core.LogEntity(Core.Server, entity);
                    count++;
                }
            }
        }
        finally
        {
            archetypeChunks.Dispose();
            // Core.Log.LogWarning($"Found {count} global patrol entities!");
        }
    }
    */
}
