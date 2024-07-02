using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Merchants.Patches;

[HarmonyPatch]
internal static class MerchantPatches
{
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    static readonly PrefabGUID NoctemBEH = new(-1999051184);
    static readonly PrefabGUID invulnerable = new(1811209060);

    [HarmonyPatch(typeof(SpawnTransformSystem_OnSpawn), nameof(SpawnTransformSystem_OnSpawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(SpawnTransformSystem_OnSpawn __instance)
    {
        NativeArray<Entity> entities = __instance.__query_565030732_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (entity.Read<PrefabGUID>().LookupName().Contains("CHAR_Trader") && entity.Read<UnitLevel>().Level._Value == 100)
                {
                    ApplyBuffDebugEvent applyBuffDebugEvent = new()
                    {
                        BuffPrefabGUID = invulnerable,
                    };
                    FromCharacter fromCharacter = new() { Character = entity, User = entity };
                    DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                    if (Core.ServerGameManager.TryGetBuff(entity, invulnerable.ToIdentifier(), out Entity buff))
                    {
                        buff.Write(new LifeTime { Duration = -1f, EndAction = LifeTimeEndAction.None });
                    }
                    if (!entity.Read<BehaviourTreeBinding>().PrefabGUID.Equals(NoctemBEH))
                    {
                        entity.Write(new BehaviourTreeBinding { PrefabGUID = NoctemBEH });
                        Core.BehaviourTreeBindingSystem_Spawn.OnUpdate();
                    }
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
