using HarmonyLib;
using ProjectM;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Penumbra.Patches;

[HarmonyPatch]
internal static class SpawnMerchantPatch
{
    static readonly PrefabGUID _noctemBEH = new(-1999051184);
    static readonly PrefabGUID _buffResistanceUberMob = new(99200653);

    static readonly PrefabGUID _noctemMajorTrader = new(1631713257);
    static readonly PrefabGUID _noctemMinorTrader = new(345283594);
    static readonly PrefabGUID _defaultEmoteBuff = new(-988102043);

    const int TRADER_LEVEL = 100;

    [HarmonyPatch(typeof(SpawnTransformSystem_OnSpawn), nameof(SpawnTransformSystem_OnSpawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(SpawnTransformSystem_OnSpawn __instance)
    {
        if (!Core._hasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_565030732_0.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if ((prefabGUID.Equals(_noctemMinorTrader) || prefabGUID.Equals(_noctemMajorTrader)) && entity.TryGetComponent(out UnitLevel unitLevel) && unitLevel.Level._Value == TRADER_LEVEL)
                {
                    entity.With((ref UnitStats unitStats) =>
                    {
                        unitStats.DamageReduction._Value = 100f;
                        unitStats.PhysicalResistance._Value = 100f;
                        unitStats.SpellResistance._Value = 100f;
                        unitStats.PvPProtected._Value = true;
                        unitStats.FireResistance._Value = 10000;
                        unitStats.PvPResilience._Value = 1;
                    });

                    entity.With((ref BuffResistances buffResistances) =>
                    {
                        buffResistances.InitialSettingGuid = _buffResistanceUberMob;
                    });

                    entity.With((ref DynamicCollision dynamicCollision) =>
                    {
                        dynamicCollision.Immobile = true;
                    });
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
