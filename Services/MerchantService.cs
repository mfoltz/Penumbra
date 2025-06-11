using Penumbra.Resources;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Penumbra.Plugin;
using ProjectM.Gameplay.Scripting;
using System.Globalization;

namespace Penumbra.Services;
internal class MerchantService
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static EntityTypeHandle EntityTypeHandle => EntityManager.GetEntityTypeHandle();
    static EntityStorageInfoLookup EntityStorageInfoLookup => EntityManager.GetEntityStorageInfoLookup();
    static ComponentTypeHandle<Immortal> ImmortalHandle => EntityManager.GetComponentTypeHandle<Immortal>(true);

    const float TIME_CONSTANT = 60f;
    const float SPAWN_DELAY = 0.25f;
    const float ROUTINE_DELAY = 15f;

    static readonly WaitForSeconds _spawnDelay = new(SPAWN_DELAY);
    static readonly WaitForSeconds _routineDelay = new(ROUTINE_DELAY);

    static readonly PrefabGUID _infiniteInvulnerabilityBuff = PrefabGUIDs.InfiniteInvulnerabilityBuff;
    static readonly PrefabGUID _buffResistanceUberMob = PrefabGUIDs.BuffResistance_UberMob_IgniteResistant;
    static readonly PrefabGUID _ignoredFaction = PrefabGUIDs.Faction_Ignored;

    static readonly ComponentType[] _merchantComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Trader>()),
        ComponentType.ReadOnly(Il2CppType.Of<Immortal>()),
        ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
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
        public string Name;
        public List<PrefabGUID> OutputItems;
        public List<int> OutputAmounts;
        public List<PrefabGUID> InputItems;
        public List<int> InputAmounts;
        public List<int> StockAmounts;
        public int RestockInterval;
        public DateTime NextRestockTime = DateTime.MaxValue;
        public int MerchantIndex;
        public bool Roam;
        public PrefabGUID TraderPrefab;
        public float3 Position;
    }

    static readonly ConcurrentDictionary<Entity, MerchantWares> _activeMerchants = [];
    public static IReadOnlyDictionary<Entity, MerchantWares> ActiveMerchants => _activeMerchants;

    static readonly List<MerchantWares> _merchantWares = [];
    public static MerchantWares GetMerchantWares(int index) => _merchantWares[index];
    public MerchantService()
    {
        _merchantQuery = EntityManager.BuildEntityQuery(_merchantComponents, options: EntityQueryOptions.IncludeDisabled);

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
            AutoSpawnMerchants();
            RestockRoutine().Start();
        }
        catch (Exception ex)
        {
            Core.Log.LogError(ex);
        }
    }
    static IEnumerator RestockRoutine()
    {
        while (true)
        {
            DateTime now = DateTime.UtcNow;

            foreach (var merchantWarePairs in ActiveMerchants)
            {
                try
                {
                    Entity merchant = merchantWarePairs.Key;
                    MerchantWares merchantWares = merchantWarePairs.Value;

                    if (!merchant.Exists())
                    {
                        _activeMerchants.TryRemove(merchant, out _);
                        continue;
                    }
                    else if (merchantWares.NextRestockTime.Equals(DateTime.MaxValue))
                    {
                        SyncNextRestock(merchant, merchantWares);
                    }
                    else if (now >= merchantWares.NextRestockTime)
                    {
                        UpdateMerchantStock(merchant, merchantWares, now);
                    }
                }
                catch (Exception ex)
                {
                    Core.Log.LogWarning($"RestockMerchantsRoutine() - {ex}");
                }

                yield return null;
            }

            yield return _routineDelay;
        }
    }
    static void PopulateMerchantWares()
    {
        DateTime now = DateTime.UtcNow;

        foreach (MerchantConfig merchantConfig in Merchants)
        {
            MerchantWares merchantWares = new()
            {
                Name = merchantConfig.Name,
                OutputItems = [..merchantConfig.OutputItems.Select(id => new PrefabGUID(int.Parse(id)))],
                OutputAmounts = [..merchantConfig.OutputAmounts],
                InputItems = [..merchantConfig.InputItems.Select(id => new PrefabGUID(int.Parse(id)))],
                InputAmounts = [..merchantConfig.InputAmounts],
                StockAmounts = [..merchantConfig.StockAmounts],
                RestockInterval = merchantConfig.RestockTime,
                MerchantIndex = Merchants.IndexOf(merchantConfig),
                Roam = merchantConfig.Roam,
                TraderPrefab = new(merchantConfig.TraderPrefab),
                Position = ParseFloat3FromString(merchantConfig.Position)
            };

            _merchantWares.Add(merchantWares);
        }
    }
    public static void SpawnMerchant(PrefabGUID traderPrefabGuid, float3 aimPosition, MerchantWares wares)
    {
        Entity merchant = ServerGameManager.InstantiateEntityImmediate(Entity.Null, traderPrefabGuid);

        ApplyOrRefreshModifications(merchant, wares);
        ModifyMerchant(merchant, aimPosition, wares).Start();
        Instance.UpdateMerchantDefinition(wares.MerchantIndex, traderPrefabGuid.GuidHash, aimPosition);
    }
    static void ApplyOrRefreshModifications(Entity merchant, MerchantWares wares)
    {
        if (merchant.Exists())
        {
            merchant.AddWith((ref EntityOwner entityOwner) =>
            {
                entityOwner.Owner = merchant;
            });

            merchant.AddWith((ref Buffable buffable) =>
            {
                buffable.KnockbackResistanceIndex._Value = 11;
            });

            merchant.AddWith((ref BuffResistances buffResistances) =>
            {
                buffResistances.InitialSettingGuid = _buffResistanceUberMob;
            });

            merchant.AddWith((ref Immortal immortal) =>
            {
                immortal.IsImmortal = true;
            });

            merchant.AddWith((ref NameableInteractable nameableInteractable) =>
            {
                nameableInteractable.Name = new(wares.Name);
            });

            merchant.With((ref DynamicCollision dynamicCollision) =>
            {
                dynamicCollision.Immobile = true;
            });

            MakeImperviousAndIgnored(merchant, wares.Roam);

            /*
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
            */
        }
    }
    static void MakeImperviousAndIgnored(Entity merchant, bool roam = false)
    {
        Entity buffEntity = ServerGameManager.InstantiateBuffEntityImmediate(merchant, merchant, _infiniteInvulnerabilityBuff);

        if (buffEntity.Exists())
        {
            if (!roam)
            {
                buffEntity.AddWith((ref ModifyMovementSpeedBuff modifyMovementSpeed) =>
                {
                    modifyMovementSpeed.MoveSpeed = 0;
                    modifyMovementSpeed.MultiplyAdd = false;
                });
            }

            buffEntity.AddWith((ref Script_Buff_ModifyFaction_DataServer modifyFaction) =>
            {
                modifyFaction.Faction = _ignoredFaction;
            });

            buffEntity.Add<ScriptSpawn>();
        }
    }
    static IEnumerator ModifyMerchant(Entity merchant, float3 aimPosition, MerchantWares wares)
    {
        yield return _spawnDelay;

        merchant.SetPosition(aimPosition);
        
        merchant.With((ref UnitStats unitStats) =>
        {
            unitStats.DamageReduction._Value = 100f;
            unitStats.PhysicalResistance._Value = 100f;
            unitStats.SpellResistance._Value = 100f;
            unitStats.HealthRecovery._Value = 1f;
            unitStats.FireResistance._Value = wares.MerchantIndex;
        });

        merchant.AddWith((ref Immortal immortal) =>
        {
            immortal.IsImmortal = true;
        });

        merchant.With((ref DynamicCollision dynamicCollision) =>
        {
            dynamicCollision.Immobile = true;
        });

        _activeMerchants.TryAdd(merchant, wares);

        DateTime now = DateTime.UtcNow;
        UpdateMerchantStock(merchant, wares, now);
    }
    static void UpdateMerchantStock(Entity merchant, MerchantWares merchantWares, DateTime now)
    {
        float restockTime = merchantWares.RestockInterval * TIME_CONSTANT;
        merchantWares.NextRestockTime = now.AddMinutes(merchantWares.RestockInterval);

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
        
        merchant.AddWith((ref Trader trader) =>
        {
            trader.RestockTime = restockTime;
            trader.PrevRestockTime = Core.ServerTime;
            trader.NextRestockTime = Core.ServerTime + (double)restockTime;
        });
    }
    static void SyncNextRestock(Entity merchant, MerchantWares merchantWares)
    {
        Trader trader = merchant.Read<Trader>();
        double now = Core.ServerTime;

        float restockTime = merchantWares.RestockInterval * TIME_CONSTANT;
        double delta = trader.NextRestockTime - trader.PrevRestockTime;

        if (!trader.RestockTime.Equals(restockTime))
        {
            merchant.With((ref Trader trader) =>
            {
                trader.RestockTime = restockTime;
            });
        }

        if (now > trader.NextRestockTime || delta > restockTime)
        {
            UpdateMerchantStock(merchant, merchantWares, DateTime.UtcNow);
        }
    }
    static void GetActiveMerchants()
    {
        NativeArray<ArchetypeChunk> archetypeChunks = _merchantQuery.CreateArchetypeChunkArray(Allocator.Temp);
        EntityStorageInfoLookup entityStorageInfoLookup = EntityStorageInfoLookup;
        int count = 0;

        try
        {
            foreach (ArchetypeChunk archetypeChunk in archetypeChunks)
            {
                NativeArray<Entity> entityArray = archetypeChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Immortal> immortalArray = archetypeChunk.GetNativeArray(ImmortalHandle);

                for (int i = 0; i < archetypeChunk.Count; i++)
                {
                    Entity entity = entityArray[i];

                    if (!entityStorageInfoLookup.Exists(entity)) continue;

                    int wares = GetMerchantIndex(entity);

                    if (wares < 0 || wares >= _merchantWares.Count)
                    {
                        // Core.Log.LogWarning($"Invalid wares index ({wares}) for Penumbra merchant, skipping! (did you remove a set of wares for an active merchant?)");
                        continue;
                    }

                    MerchantWares merchantWares = GetMerchantWares(wares);
                    ApplyOrRefreshModifications(entity, merchantWares);

                    _activeMerchants.TryAdd(entity, merchantWares);
                    count++;
                }
            }
        }
        finally
        {
            archetypeChunks.Dispose();
            Core.Log.LogWarning($"Tracking {count} Penumbra merchants found in world!");
        }
    }
    static int GetMerchantIndex(Entity merchant)
    {
        if (merchant.TryGetComponent(out UnitStats unitStats))
        {
            return unitStats.FireResistance._Value;
        }

        return -1;
    }
    static float3 ParseFloat3FromString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return float3.zero;
        }

        string[] parts = configString.Split(',');
        if (parts.Length != 3)
        {
            throw new FormatException("Invalid float3 string format. Expected format: 'x,y,z'.");
        }

        return new float3(
            float.Parse(parts[0], CultureInfo.InvariantCulture),
            float.Parse(parts[1], CultureInfo.InvariantCulture),
            float.Parse(parts[2], CultureInfo.InvariantCulture)
        );
    }
    static void AutoSpawnMerchants()
    {
        for (int i = 0; i < _merchantWares.Count; i++)
        {
            var wares = _merchantWares[i];

            string merchantName = wares.Name;
            bool found = false;

            if (wares.TraderPrefab.IsEmpty() || wares.Position.Equals(float3.zero))
                continue;

            foreach (var kvp in _activeMerchants)
            {
                if (kvp.Key.TryGetComponent(out NameableInteractable nameableInteractable) && merchantName.Equals(nameableInteractable.Name.Value))
                {
                     found = true;
                }
            }

            if (!found)
            {
                Core.Log.LogInfo($"[MerchantService] Auto-spawning merchant " +
                                 $"#{i + 1} ({wares.TraderPrefab.GetPrefabName()}) at {wares.Position}");
                SpawnMerchant(wares.TraderPrefab, wares.Position, wares);
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
