using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Penumbra.Patches;

[HarmonyPatch]
internal static class WeaponAbilityPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly WaitForSeconds DestroyGroundSwordDelay = new(60f);

    static readonly PrefabGUID HighLordLeapEndBuff = new(1836176758);
    static readonly PrefabGUID HighLordPermaBuff = new(-916946628);

    static readonly PrefabGUID HighLordGroundSword = new(-1266036232);

    static readonly PrefabGUID FleshWarp = new(2145809434);
    static readonly PrefabGUID CorpseStorm = new(1006960825);

    static readonly PrefabGUID DeathTimerBuff = new(1273155981);
    
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
                else if (entity.TryGetComponent(out PrefabGUID buffPrefab) && buffPrefab.Equals(HighLordLeapEndBuff))
                {
                    if (entity.Has<SpawnMinionOnGameplayEvent>()) entity.Remove<SpawnMinionOnGameplayEvent>();
                    if (entity.Has<CreateGameplayEventsOnSpawn>()) entity.Remove<CreateGameplayEventsOnSpawn>();
                    if (entity.Has<CreateGameplayEventsOnDestroy>()) entity.Remove<CreateGameplayEventsOnDestroy>();
                    if (entity.Has<ApplyBuffOnGameplayEvent>()) entity.Remove<ApplyBuffOnGameplayEvent>();
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    /*
    [HarmonyPatch(typeof(LinkMinionToOwnerOnSpawnSystem), nameof(LinkMinionToOwnerOnSpawnSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(LinkMinionToOwnerOnSpawnSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.IsPlayer()) continue;
                else if (entity.TryGetComponent(out PrefabGUID prefabGUID) && prefabGUID.Equals(HighLordGroundSword))
                {
                    Core.Log.LogInfo("HighLordGroundSword in LinkMinionToOwnerOnSpawnSystem...");
                    Utilities.BuffUtilities.TryApplyBuff(entity, DeathTimerBuff);

                    entity.With((ref Aggroable aggroable) =>
                    {
                        aggroable.AggroFactor._Value = 3f;
                    });

                    if (entity.Has<Interactable>()) entity.Remove<Interactable>();
                    if (entity.Has<InteractAbilityBuffer>()) entity.Remove<InteractAbilityBuffer>();

                    if (ServerGameManager.TryGetBuffer<AbilityGroupSlotBuffer>(entity, out var buffer) && buffer.IsIndexWithinRange(1)) buffer.RemoveAt(1);

                    Core.StartCoroutine(DelayedGroundSwordDestroy(entity));
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    */

    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReplaceAbilityOnSlotSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.IsPlayer()) continue;
                else if (entity.TryGetComponent(out EquippableBuff equippableBuff) 
                    && equippableBuff.ItemSource.TryGetComponent(out PrefabGUID itemPrefabGUID)
                    && Core.ShadowMatterWeapons.Contains(itemPrefabGUID))
                {
                    //Core.Log.LogInfo("ShadowMatterWeapon in ReplaceAbilityOnSlotSystem...");
                    List<PrefabGUID> ShadowMatterAbilities = Core.ShadowMatterAbilitiesMap[itemPrefabGUID];

                    if (ServerGameManager.TryGetBuffer<ReplaceAbilityOnSlotBuff>(entity, out var buffer))
                    {
                        for (int i = 0; i < ShadowMatterAbilities.Count; i++)
                        {
                            PrefabGUID abilityPrefab = ShadowMatterAbilities[i];

                            if (i == 2) i += 2;
                            else if (i == 0) continue;

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
                /*
                else if (entity.TryGetComponent(out PrefabGUID prefabGUID) && prefabGUID.Equals(HighLordPermaBuff))
                {
                    Core.Log.LogInfo("HighLordPermaBuff in ReplaceAbilityOnSlotSystem...");

                    if (ServerGameManager.TryGetBuffer<ReplaceAbilityOnSlotBuff>(entity, out var buffer) && buffer.IsIndexWithinRange(1))
                    {
                        ReplaceAbilityOnSlotBuff replaceAbilityOnSlotBuff = buffer[1];

                        replaceAbilityOnSlotBuff.Slot = 1;
                        replaceAbilityOnSlotBuff.NewGroupId = FleshWarp;
                        replaceAbilityOnSlotBuff.CopyCooldown = true;
                        replaceAbilityOnSlotBuff.Priority = 0;

                        buffer[1] = replaceAbilityOnSlotBuff;

                        replaceAbilityOnSlotBuff.Slot = 4;
                        replaceAbilityOnSlotBuff.NewGroupId = CorpseStorm;
                        replaceAbilityOnSlotBuff.CopyCooldown = true;
                        replaceAbilityOnSlotBuff.Priority = 0;

                        buffer.Add(replaceAbilityOnSlotBuff);
                    }

                    entity.With((ref AmplifyBuff amplifyBuff) =>
                    {
                        amplifyBuff.AmplifyModifier = -0.5f;
                    });
                }
                */
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
                    PrefabGUID abilityGroupPrefabGUID = abilityPostCastFinishedEvent.AbilityGroup.GetPrefabGUID();

                    if (Core.AbilityPrefabGUIDs.ContainsKey(abilityGroupPrefabGUID) && ServerGameManager.TryGetBuffer<AbilityStateBuffer>(abilityPostCastFinishedEvent.AbilityGroup, out var buffer) && !buffer.IsEmpty)
                    {
                        Entity abilityGroupCast = buffer[0].StateEntity.GetEntityOnServer();
                        PrefabGUID abilityGroupCastPrefabGUID = abilityGroupCast.GetPrefabGUID();

                        // ServerGameManager.GetAbilityGroupCooldown(abilityPostCastFinishedEvent.Character, abilityGroupPrefabGUID); should probably do this instead but don't feel like verifying it will work right now

                        if (abilityGroupCast.TryGetComponent(out AbilityCooldownData abilityCooldownData) 
                            && Core.AbilityPrefabGUIDs.TryGetValue(abilityGroupCastPrefabGUID, out float cooldown) 
                            && abilityCooldownData.Cooldown._Value != cooldown)
                        {

                            abilityGroupCast.With((ref AbilityCooldownData abilityCooldownData) =>
                            {
                                abilityCooldownData.Cooldown._Value = cooldown;
                            });

                            ServerGameManager.SetAbilityGroupCooldown(abilityPostCastFinishedEvent.Character, abilityGroupPrefabGUID, cooldown);
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
}
