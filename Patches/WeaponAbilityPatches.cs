using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Merchants.Patches;


[HarmonyPatch]
internal static class WeaponAbilityPatches
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID highlordLeapEndBuff = new(1836176758);
    static readonly PrefabGUID highlordLeapAbilityGroup = new(938684260);
    static readonly PrefabGUID highlordDashAbilityGroup = new(-2126197617);
    static readonly PrefabGUID bloodknightLeapAbilityGroup = new(1826128809);
    static readonly PrefabGUID bloodknightTwirlAbilityGroup = new(1730729556);

    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReplaceAbilityOnSlotSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp); // All Components: ProjectM.EntityOwner [ReadOnly], ProjectM.Buff [ReadOnly], ProjectM.ReplaceAbilityOnSlotData [ReadOnly], ProjectM.ReplaceAbilityOnSlotBuff [Buffer] [ReadOnly], Unity.Entities.SpawnTag [ReadOnly]
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.GetOwner().IsPlayer() && entity.TryGetComponent(out EquippableBuff equippableBuff))
                {
                    if (equippableBuff.ItemSource.TryGetComponent(out PrefabGUID itemPrefab))
                    {
                        if (Core.ShadowMatterWeapons.Contains(itemPrefab))
                        {
                            var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();

                            for (int i = 0; i < Core.ShadowMatterAbilitiesMap[itemPrefab].Count; i++)
                            {
                                PrefabGUID abilityPrefab = Core.ShadowMatterAbilitiesMap[itemPrefab][i];
                                if (i == 2) i += 2;

                                ReplaceAbilityOnSlotBuff buff = new()
                                {
                                    Slot = i,
                                    NewGroupId = abilityPrefab,
                                    CopyCooldown = true,
                                    Priority = 0,
                                };

                                buffer.Add(buff);
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.GetBuffTarget().IsPlayer()) continue;
                else if (entity.TryGetComponent(out PrefabGUID buffPrefab))
                {
                    if (buffPrefab.Equals(highlordLeapEndBuff))
                    {
                        if (entity.Has<SpawnMinionOnGameplayEvent>()) entity.Remove<SpawnMinionOnGameplayEvent>();
                        if (entity.Has<CreateGameplayEventsOnSpawn>()) entity.Remove<CreateGameplayEventsOnSpawn>();
                        if (entity.Has<CreateGameplayEventsOnDestroy>()) entity.Remove<CreateGameplayEventsOnDestroy>();
                        if (entity.Has<ApplyBuffOnGameplayEvent>()) entity.Remove<ApplyBuffOnGameplayEvent>();
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._OnPostCastFinishedQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.TryGetComponent(out AbilityPostCastFinishedEvent abilityPostCastFinishedEvent) && abilityPostCastFinishedEvent.Character.IsPlayer())
                {
                    PrefabGUID abilityPrefab = abilityPostCastFinishedEvent.AbilityGroup.Read<PrefabGUID>();
                    if (abilityPrefab.Equals(highlordDashAbilityGroup))
                    {
                        ServerGameManager.SetAbilityGroupCooldown(abilityPostCastFinishedEvent.Character, abilityPrefab, 8f);
                    }
                    else if (abilityPrefab.Equals(highlordLeapAbilityGroup))
                    {
                        ServerGameManager.SetAbilityGroupCooldown(abilityPostCastFinishedEvent.Character, abilityPrefab, 8f);
                    }
                    else if (abilityPrefab.Equals(bloodknightLeapAbilityGroup))
                    {
                        ServerGameManager.SetAbilityGroupCooldown(abilityPostCastFinishedEvent.Character, abilityPrefab, 8f);
                    }
                    else if (abilityPrefab.Equals(bloodknightTwirlAbilityGroup))
                    {
                        ServerGameManager.SetAbilityGroupCooldown(abilityPostCastFinishedEvent.Character, abilityPrefab, 8f);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }  
}
