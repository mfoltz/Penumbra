using HarmonyLib;
using ProjectM.Behaviours;
using ProjectM.Shared.Systems;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Penumbra.Patches;

[HarmonyPatch]
internal static class SpawnMerchantPatch
{
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;

    static readonly PrefabGUID NoctemBEH = new(-1999051184);
    static readonly PrefabGUID InvulnerableBuff = new(-480024072);

    static readonly PrefabGUID NoctemMajorTrader = new(1631713257);

    static readonly Random Random = new();

    static readonly Dictionary<ulong, PrefabGUID> PlayerPurchases = [];

    [HarmonyPatch(typeof(SpawnTransformSystem_OnSpawn), nameof(SpawnTransformSystem_OnSpawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(SpawnTransformSystem_OnSpawn __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_565030732_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if (NoctemMajorTrader.Equals(prefabGUID) && entity.TryGetComponent(out UnitLevel unitLevel) && unitLevel.Level._Value == 100)
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
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
}
