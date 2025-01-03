using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Penumbra.Patches;

[HarmonyPatch]
internal static class JewelPatches
{
    static NetworkIdSystem.Singleton NetworkIdSystem => Core.NetworkIdSystem;
    static SpellModSyncSystem_Server SpellModSyncSystemServer => Core.SpellModSyncSystem_Server;

    static readonly Random Random = new();

    static readonly Dictionary<ulong, PrefabGUID> PlayerPurchases = [];

    [HarmonyPatch(typeof(TraderPurchaseSystem), nameof(TraderPurchaseSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(TraderPurchaseSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._TraderPurchaseEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                TraderPurchaseEvent traderPurchaseEvent = entity.Read<TraderPurchaseEvent>();

                FromCharacter fromCharacter = entity.Read<FromCharacter>();
                ulong steamId = fromCharacter.User.Read<User>().PlatformId;

                Entity trader = NetworkIdSystem._NetworkIdLookupMap._NetworkIdToEntityMap[traderPurchaseEvent.Trader];
                var outputBuffer = trader.ReadBuffer<TradeOutput>();

                PrefabGUID item = outputBuffer[traderPurchaseEvent.ItemIndex].Item;
                string itemName = item.LookupName();

                if (itemName.Contains("Item_Jewel"))
                {
                    if (!PlayerPurchases.ContainsKey(steamId))
                    {
                        PlayerPurchases.TryAdd(steamId, item);
                    }
                    else
                    {
                        PlayerPurchases[steamId] = item;
                    }
                }
            }
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
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._JewelSpawnQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGUID) 
                    || !entity.TryGetComponent(out InventoryItem inventoryItem) 
                    || !inventoryItem.ContainerEntity.TryGetComponent(out InventoryConnection inventoryConnection) 
                    || !inventoryConnection.InventoryOwner.TryGetPlayer(out Entity player)) continue;

                ulong steamId = player.GetSteamId();
                if (PlayerPurchases.TryGetValue(steamId, out var playerPurchases) 
                    && playerPurchases.Equals(prefabGUID) 
                    && entity.TryGetComponent(out SpellModSetComponent spellModSetComponent)
                    && entity.TryGetComponent(out JewelInstance jewelInstance))
                {
                    PlayerPurchases.Remove(steamId);

                    PrefabGUID abilityGroup = jewelInstance.OverrideAbilityType;
                    if (abilityGroup.IsEmpty()) abilityGroup = jewelInstance.Ability;

                    List<PrefabGUID> spellMods = Core.SpellModSets[abilityGroup];
                    List<PrefabGUID> usedSpellMods = [];

                    SpellModSet spellModSet = spellModSetComponent.SpellMods;

                    if (spellMods.Contains(spellModSet.Mod0.Id)) usedSpellMods.Add(spellModSet.Mod0.Id);
                    if (spellMods.Contains(spellModSet.Mod1.Id)) usedSpellMods.Add(spellModSet.Mod1.Id);
                    if (spellMods.Contains(spellModSet.Mod2.Id)) usedSpellMods.Add(spellModSet.Mod2.Id);
                    if (spellMods.Contains(spellModSet.Mod3.Id)) usedSpellMods.Add(spellModSet.Mod3.Id);
                    if (spellMods.Contains(spellModSet.Mod4.Id)) usedSpellMods.Add(spellModSet.Mod4.Id);
                    if (spellMods.Contains(spellModSet.Mod5.Id)) usedSpellMods.Add(spellModSet.Mod5.Id);
                    if (spellMods.Contains(spellModSet.Mod6.Id)) usedSpellMods.Add(spellModSet.Mod6.Id);
                    if (spellMods.Contains(spellModSet.Mod7.Id)) usedSpellMods.Add(spellModSet.Mod7.Id);

                    // Get the unused spell mods
                    List<PrefabGUID> unusedSpellMods = spellMods.Except(usedSpellMods).ToList();

                    // Shuffle the list
                    unusedSpellMods.Shuffle();

                    // Calculate available slots
                    int availableSlots = 8 - spellModSet.Count;

                    // Select up to availableSlots spell mods from the shuffled list
                    List<PrefabGUID> newSpellMods = unusedSpellMods.Take(availableSlots).ToList();

                    // Assign the new spell mods to available Mod slots (Mod4 to Mod7)
                    int assignedMods = 0;
                    for (int i = 4; i < 8 && assignedMods < newSpellMods.Count; i++)
                    {
                        PrefabGUID spellModPrefabGUID = newSpellMods[assignedMods];
                        float powerValue = GetPowerValueForSpellMod(spellModPrefabGUID);

                        switch (i)
                        {
                            case 4:
                                if (spellModSet.Mod4.Id.IsEmpty())
                                {
                                    spellModSet.Mod4.Id = spellModPrefabGUID;
                                    spellModSet.Mod4.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                            case 5:
                                if (spellModSet.Mod5.Id.IsEmpty())
                                {
                                    spellModSet.Mod5.Id = spellModPrefabGUID;
                                    spellModSet.Mod5.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                            case 6:
                                if (spellModSet.Mod6.Id.IsEmpty())
                                {
                                    spellModSet.Mod6.Id = spellModPrefabGUID;
                                    spellModSet.Mod6.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                            case 7:
                                if (spellModSet.Mod7.Id.IsEmpty())
                                {
                                    spellModSet.Mod7.Id = spellModPrefabGUID;
                                    spellModSet.Mod7.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                        }
                    }

                    // Update the spellModSet.Count
                    spellModSet.Count += (byte)newSpellMods.Count;
                    if (spellModSet.Count > 8) spellModSet.Count = 8;

                    SpellModSyncSystemServer.AddSpellMod(ref spellModSet);
                    spellModSetComponent.SpellMods = spellModSet;

                    entity.Write(spellModSetComponent);

                    SpellModSyncSystemServer.OnUpdate();
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    static float GetPowerValueForSpellMod(PrefabGUID spellModPrefabGUID)
    {
        if (Core.SpellModPowerRanges.TryGetValue(spellModPrefabGUID, out var powerRange))
        {
            if (powerRange.HasValue)
            {
                double minPower = powerRange.Value.Min;
                double maxPower = powerRange.Value.Max;

                // Skew factor less than 1 skews towards maxPower
                double skewFactor = 0.25; // Adjust this value between 0 and 1
                double skewedRandom = Math.Pow(Random.NextDouble(), skewFactor);
                double randomValue = minPower + skewedRandom * (maxPower - minPower);

                return (float)randomValue;
            }
            else
            {
                return 1f;
            }
        }
        else
        {
            return 1f;
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
static void AssignUnusedSpellMod(ref SpellModSet spellModSet, PrefabGUID unusedSpellMod, int spellModIndex)
{
    switch (spellModIndex)
    {
        case 4:
            if (spellModSet.Mod4.Id.IsEmpty())
            {
                spellModSet.Mod4.Id = unusedSpellMod;
                spellModSet.Mod4.Power = (float)Random.NextDouble();
            }

            break;
        case 5:
            if (spellModSet.Mod5.Id.IsEmpty())
            {
                spellModSet.Mod5.Id = unusedSpellMod;
                spellModSet.Mod5.Power = (float)Random.NextDouble();
            }

            break;
        case 6:
            if (spellModSet.Mod6.Id.IsEmpty())
            {
                spellModSet.Mod6.Id = unusedSpellMod;
                spellModSet.Mod6.Power = (float)Random.NextDouble();
            }

            break;
        case 7:
            if (spellModSet.Mod7.Id.IsEmpty())
            {
                spellModSet.Mod7.Id = unusedSpellMod;
                spellModSet.Mod7.Power = (float)Random.NextDouble();
            }

            break;
    }
}
*/
