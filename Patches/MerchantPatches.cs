using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Network;
using ProjectM.Shared;
using ProjectM.Shared.Systems;
using Steamworks;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Merchants.Patches;

[HarmonyPatch]
internal static class MerchantPatches
{
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;

    static readonly PrefabGUID NoctemBEH = new(-1999051184);
    static readonly PrefabGUID invulnerable = new(-480024072);

    static readonly Random Random = new();

    static readonly Dictionary<ulong, PrefabGUID> PlayerPurchases = [];

    [HarmonyPatch(typeof(SpawnTransformSystem_OnSpawn), nameof(SpawnTransformSystem_OnSpawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(SpawnTransformSystem_OnSpawn __instance)
    {
        NativeArray<Entity> entities = __instance.__query_565030732_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;

                if (entity.Read<PrefabGUID>().LookupName().Contains("CHAR_Trader") && entity.Read<UnitLevel>().Level._Value == 100)
                {
                    UnitStats unitStats = entity.Read<UnitStats>();
                    unitStats.DamageReduction._Value = 100f;
                    unitStats.PhysicalResistance._Value = 100f;
                    unitStats.SpellResistance._Value = 100f;
                    unitStats.PvPProtected._Value = true;
                    unitStats.FireResistance._Value = 10000;
                    entity.Write(unitStats);

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

    [HarmonyPatch(typeof(TraderPurchaseSystem), nameof(TraderPurchaseSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(TraderPurchaseSystem __instance)
    {
        NativeArray<Entity> entities = __instance._TraderPurchaseEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;
                TraderPurchaseEvent traderPurchaseEvent = entity.Read<TraderPurchaseEvent>();

                FromCharacter fromCharacter = entity.Read<FromCharacter>();
                ulong steamId = fromCharacter.User.Read<User>().PlatformId;

                Entity trader = Core.NetworkIdSystem._NetworkIdLookupMap._NetworkIdToEntityMap[traderPurchaseEvent.Trader];
                var outputBuffer = trader.ReadBuffer<TradeOutput>();

                PrefabGUID item = outputBuffer[traderPurchaseEvent.ItemIndex].Item;
                string itemName = item.LookupName();

                if (itemName.Contains("Item_Jewel"))
                {
                    if (!PlayerPurchases.ContainsKey(steamId))
                    {
                        PlayerPurchases.Add(steamId, item);
                    }
                    else
                    {
                        PlayerPurchases[steamId] = item;
                    }
                }
                /*
                else if (itemName.Contains("ShadowMatter"))
                {
                    if (!PlayerPurchases.ContainsKey(steamId))
                    {
                        PlayerPurchases.Add(steamId, item);
                    }
                    else
                    {
                        PlayerPurchases[steamId] = item;
                    }
                }
                */
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

    /*
    [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReactToInventoryChangedSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_2096870024_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                if (!Core.hasInitialized) return;

                InventoryChangedEvent inventoryChangedEvent = entity.Read<InventoryChangedEvent>();
                Entity inventory = inventoryChangedEvent.InventoryEntity;

                if (!inventory.Exists()) continue;

                if (inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>())
                {
                    InventoryConnection inventoryConnection = inventory.Read<InventoryConnection>();
                    if (!inventoryConnection.InventoryOwner.Has<UserOwner>()) continue;

                    UserOwner userOwner = inventoryConnection.InventoryOwner.Read<UserOwner>();
                    Entity userEntity = userOwner.Owner._Entity;
                    if (!userEntity.Exists()) continue;

                    PrefabGUID itemPrefab = inventoryChangedEvent.Item;
                    ulong steamId = userEntity.Read<User>().PlatformId;

                    if (PlayerPurchases.ContainsKey(steamId) && PlayerPurchases[steamId].Equals(itemPrefab))
                    {
                        PlayerPurchases.Remove(steamId);
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
    */

    [HarmonyPatch(typeof(JewelSpawnSystem), nameof(JewelSpawnSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(JewelSpawnSystem __instance)
    {
        NativeArray<Entity> entities = __instance._JewelSpawnQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;

                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                //Core.Log.LogInfo($"Jewel item: {prefabGUID.LookupName()}");
                if (!entity.Has<InventoryItem>()) continue;
                //Core.Log.LogInfo("Check1");
                if (entity.Read<InventoryItem>().ContainerEntity.Equals(Entity.Null)) continue;
                //Core.Log.LogInfo("Check2");
                if (!entity.Read<InventoryItem>().ContainerEntity.Has<InventoryConnection>()) continue;
                //Core.Log.LogInfo("Check3");
                if (!entity.Read<InventoryItem>().ContainerEntity.Read<InventoryConnection>().InventoryOwner.Has<PlayerCharacter>()) continue;
                ulong steamId = entity.Read<InventoryItem>().ContainerEntity.Read<InventoryConnection>().InventoryOwner.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                if (PlayerPurchases.ContainsKey(steamId) && PlayerPurchases[steamId].Equals(prefabGUID))
                {
                    PlayerPurchases.Remove(steamId);
                    //Core.Log.LogInfo($"Player {steamId} has obtained {prefabGUID.LookupName()}");
                    if (entity.Has<SpellModSetComponent>())
                    {
                        PrefabGUID abilityGroup = entity.Read<JewelInstance>().OverrideAbilityType;
                        if (abilityGroup.GuidHash.Equals(0)) abilityGroup = entity.Read<JewelInstance>().Ability;
                        //Core.Log.LogInfo($"Check4 {abilityGroup.LookupName()}");
                        List<PrefabGUID> spellMods = Core.spellModSets[abilityGroup];
                        List<PrefabGUID> usedSpellMods = [];
                        SpellModSetComponent spellModSetComponent = entity.Read<SpellModSetComponent>();
                        SpellModSet spellModSet = spellModSetComponent.SpellMods;
                        if (spellMods.Contains(spellModSet.Mod0.Id)) usedSpellMods.Add(spellModSet.Mod0.Id);
                        if (spellMods.Contains(spellModSet.Mod1.Id)) usedSpellMods.Add(spellModSet.Mod1.Id);
                        if (spellMods.Contains(spellModSet.Mod2.Id)) usedSpellMods.Add(spellModSet.Mod2.Id);
                        if (spellMods.Contains(spellModSet.Mod3.Id)) usedSpellMods.Add(spellModSet.Mod3.Id);
                        if (spellMods.Contains(spellModSet.Mod4.Id)) usedSpellMods.Add(spellModSet.Mod4.Id);
                        if (spellMods.Contains(spellModSet.Mod5.Id)) usedSpellMods.Add(spellModSet.Mod5.Id);
                        if (spellMods.Contains(spellModSet.Mod6.Id)) usedSpellMods.Add(spellModSet.Mod6.Id);
                        if (spellMods.Contains(spellModSet.Mod7.Id)) usedSpellMods.Add(spellModSet.Mod7.Id);
                        //Core.Log.LogInfo("Check5");
                        List<PrefabGUID> unusedSpellMods = spellMods.Except(usedSpellMods).ToList();
                        spellModSet.Count += (byte)unusedSpellMods.Count;
                        if (spellModSet.Count > 8) spellModSet.Count = 8;
                        for (int i = 4; i < 8; i++)
                        {
                            if (i - 4 < unusedSpellMods.Count)
                            {
                                AssignUnusedSpellMod(ref spellModSet, unusedSpellMods[i - 4], i);
                            }
                        }
                        //Core.Log.LogInfo("Check6");
                        Core.SpellModSyncSystem_Server.AddSpellMod(ref spellModSet);
                        //Core.Log.LogInfo("Check7");
                        spellModSetComponent.SpellMods = spellModSet;
                        entity.Write(spellModSetComponent);
                        //Core.Log.LogInfo("Check8");
                        Core.SpellModSyncSystem_Server.OnUpdate();
                        //Core.Log.LogInfo("Check9");
                    }
                }
            }
            /*
            entities = __instance._LegendaryItemSpawnQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                Core.Log.LogInfo($"Legendary item: {prefabGUID.LookupName()}");
                if (!entity.Read<InventoryItem>().ContainerEntity.Read<InventoryConnection>().InventoryOwner.Has<PlayerCharacter>()) continue;
                ulong steamId = entity.Read<InventoryItem>().ContainerEntity.Read<InventoryConnection>().InventoryOwner.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                if (playerPurchases.ContainsKey(steamId) && playerPurchases[steamId].Equals(prefabGUID))
                {
                    playerPurchases.Remove(steamId);
                    Core.Log.LogInfo($"Player {steamId} has obtained {prefabGUID.LookupName()}");
                    if (entity.Has<LegendaryItemSpellModSetComponent>())
                    {
                        List<PrefabGUID> statMods = Core.statModSets;
                        List<PrefabGUID> usedStatMods = [];
                        LegendaryItemSpellModSetComponent spellModSetComponent = entity.Read<LegendaryItemSpellModSetComponent>();
                        SpellModSet statModSet = spellModSetComponent.StatMods;
                        if (statMods.Contains(statModSet.Mod0.Id)) usedStatMods.Add(statModSet.Mod0.Id);
                        if (statMods.Contains(statModSet.Mod1.Id)) usedStatMods.Add(statModSet.Mod1.Id);
                        if (statMods.Contains(statModSet.Mod2.Id)) usedStatMods.Add(statModSet.Mod2.Id);
                        if (statMods.Contains(statModSet.Mod3.Id)) usedStatMods.Add(statModSet.Mod3.Id);
                        if (statMods.Contains(statModSet.Mod4.Id)) usedStatMods.Add(statModSet.Mod4.Id);
                        if (statMods.Contains(statModSet.Mod5.Id)) usedStatMods.Add(statModSet.Mod5.Id);
                        if (statMods.Contains(statModSet.Mod6.Id)) usedStatMods.Add(statModSet.Mod6.Id);
                        if (statMods.Contains(statModSet.Mod7.Id)) usedStatMods.Add(statModSet.Mod7.Id);
                        List<PrefabGUID> unusedStatMods = statMods.Except(usedStatMods).ToList();
                        statModSet.Count += (byte)8;
                        for (int i = 4; i < 8; i++)
                        {
                            int index = Random.Next(unusedStatMods.Count);
                            AssignUnusedSpellMod(ref statModSet, unusedStatMods[index], i);
                            unusedStatMods.RemoveAt(index);
                        }
                        Core.SpellModSyncSystem_Server.AddSpellMod(ref statModSet);
                        spellModSetComponent.StatMods = statModSet;
                        entity.Write(spellModSetComponent);
                        Core.SpellModSyncSystem_Server.OnUpdate();
                    }
                }
            }
            */
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
    static void AssignUnusedSpellMod(ref SpellModSet spellModSet, PrefabGUID unusedSpellMod, int modIndex)
    {
        switch (modIndex)
        {
            case 4:
                if (spellModSet.Mod4.Id.GuidHash == 0)
                {
                    spellModSet.Mod4.Id = unusedSpellMod;
                    spellModSet.Mod4.Power = (float)Random.NextDouble();
                }
                break;
            case 5:
                if (spellModSet.Mod5.Id.GuidHash == 0)
                {
                    spellModSet.Mod5.Id = unusedSpellMod;
                    spellModSet.Mod5.Power = (float)Random.NextDouble();
                }
                break;
            case 6:
                if (spellModSet.Mod6.Id.GuidHash == 0)
                {
                    spellModSet.Mod6.Id = unusedSpellMod;
                    spellModSet.Mod6.Power = (float)Random.NextDouble();
                }
                break;
            case 7:
                if (spellModSet.Mod7.Id.GuidHash == 0)
                {
                    spellModSet.Mod7.Id = unusedSpellMod;
                    spellModSet.Mod7.Power = (float)Random.NextDouble();
                }
                break;
        }
    }
}
