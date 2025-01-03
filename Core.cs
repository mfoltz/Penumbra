using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Penumbra.Service;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using ProjectM.Shared;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Penumbra;
internal static class Core
{
    public static World Server { get; } = GetWorld("Server") ?? throw new Exception("There is no Server world (yet)...");
    public static EntityManager EntityManager { get; } = Server.EntityManager;
    public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }
    public static BehaviourTreeBindingSystem_Spawn BehaviourTreeBindingSystem_Spawn { get; internal set; }
    public static JewelSpawnSystem JewelSpawnSystem { get; internal set; }
    public static SpellModSpawnSystem SpellModSpawnSystem { get; internal set; }
    public static SpellModSyncPersistenceSystem SpellModSyncPersistenceSystem { get; internal set; }
    public static SpellModSyncSystem_Server SpellModSyncSystem_Server { get; internal set; }
    public static SpellModTierCollectionSystem SpellModTierCollectionSystem { get; internal set; }
    public static ServerGameSettingsSystem ServerGameSettingsSystem { get; internal set; }
    public static ServerScriptMapper ServerScriptMapper { get; internal set; }
    public static DebugEventsSystem DebugEventsSystem { get; internal set; }
    public static EntityCommandBufferSystem EntityCommandBufferSystem { get; internal set; }
    public static GameDataSystem GameDataSystem { get; internal set; }
    public static ServerGameManager ServerGameManager => ServerScriptMapper.GetServerGameManager();
    public static NetworkIdSystem.Singleton NetworkIdSystem { get; internal set; }
    public static ScriptSpawnServer ScriptSpawnServer { get; internal set; }
    public static ServerGameSettings ServerGameSettings { get; internal set; }
    public static SpellSchoolProgressionEventSystem SpellSchoolProgressionEventSystem { get; internal set; }
    public static double ServerTime => ServerGameManager.ServerTime;
    public static ManualLogSource Log => Plugin.LogInstance;

    static MonoBehaviour monoBehaviour;

    public static bool hasInitialized;
    public static void Initialize()
    {
        if (hasInitialized) return;

        // Initialize systems
        PrefabCollectionSystem = Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        ServerGameSettingsSystem = Server.GetExistingSystemManaged<ServerGameSettingsSystem>();
        DebugEventsSystem = Server.GetExistingSystemManaged<DebugEventsSystem>();
        ServerScriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();
        EntityCommandBufferSystem = Server.GetExistingSystemManaged<EntityCommandBufferSystem>();
        JewelSpawnSystem = Server.GetExistingSystemManaged<JewelSpawnSystem>();
        SpellModSpawnSystem = Server.GetExistingSystemManaged<SpellModSpawnSystem>();
        SpellModSyncPersistenceSystem = Server.GetExistingSystemManaged<SpellModSyncPersistenceSystem>();
        SpellModSyncSystem_Server = Server.GetExistingSystemManaged<SpellModSyncSystem_Server>();
        SpellModTierCollectionSystem = Server.GetExistingSystemManaged<SpellModTierCollectionSystem>();
        GameDataSystem = Server.GetExistingSystemManaged<GameDataSystem>();
        NetworkIdSystem = ServerScriptMapper.GetSingleton<NetworkIdSystem.Singleton>();
        ScriptSpawnServer = Server.GetExistingSystemManaged<ScriptSpawnServer>();
        SpellSchoolProgressionEventSystem = Server.GetExistingSystemManaged<SpellSchoolProgressionEventSystem>();
        ServerGameSettings = ServerGameSettingsSystem._Settings;

        ModifyShadowWeaponsAndAbilities();
        _ = new MerchantService();

        hasInitialized = true;
    }
    static World GetWorld(string name)
    {
        foreach (var world in World.s_AllWorlds)
        {
            if (world.Name == name)
            {
                return world;
            }
        }
        return null;
    }
    public static void StartCoroutine(IEnumerator routine)
    {
        if (monoBehaviour == null)
        {
            var go = new GameObject("Penumbra");
            monoBehaviour = go.AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
        monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }

    public static readonly Dictionary<PrefabGUID, float> AbilityPrefabGUIDs = new()
    {
        { new(938684260), 8f }, // HighLord_LeapStrike group
        { new(900477321), 8f }, // HighLord_LeapStrike cast
        { new(-2126197617), 8f }, // HighLord_SwordDashCleave group
        { new(1286990437), 8f }, // HighLord_SwordDashCleave cast
        { new(1826128809), 8f }, // BloodKnight_SkeweringLeap group
        { new(-517090394), 8f }, // BloodKnight_SkeweringLeap cast
        { new(1730729556), 8f }, // BloodKnight_SpearTwirl group
        { new(1317898066), 8f }, // BloodKnight_SpearTwirl cast
        //{ new(2145809434), 8f }, // FleshWarp group
        //{ new(1368776559), 8f }, // FleshWarp cast
        //{ new(1006960825), 20f }, // CorpseStorm group
        //{ new(2016926455), 20f}, // CorpseStorm cast
    };
    static void ModifyShadowWeaponsAndAbilities()
    {
        foreach (PrefabGUID abilityGroupCast in AbilityPrefabGUIDs.Keys)
        {
            if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(abilityGroupCast, out Entity abilityEntity) && abilityEntity.Has<AbilityCooldownData>())
            {
                abilityEntity.With((ref AbilityCooldownData abilityCooldownData) =>
                {
                    abilityCooldownData.Cooldown._Value = AbilityPrefabGUIDs[abilityGroupCast];
                });
            }
        }

        foreach (PrefabGUID shadowWeapon in ShadowMatterWeapons)
        {
            Entity prefabEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[shadowWeapon];

            if (prefabEntity.Has<ModifyUnitStatBuff_DOTS>())
            {
                var buffer = prefabEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>();

                for (int i = 0; i < buffer.Length; i++)
                {
                    ModifyUnitStatBuff_DOTS statBuff = buffer[i];

                    if (i == 0)
                    {
                        float value = statBuff.Value + 7f;
                        statBuff.Value = (int)value;
                        buffer[i] = statBuff;
                    }
                    else if (i == 1)
                    {
                        if (statBuff.StatType.Equals(UnitStatType.ResourcePower))
                        {
                            statBuff.Value += 6f;
                            buffer[i] = statBuff;
                        }
                        else
                        {
                            statBuff.Value += 0.03f;
                            buffer[i] = statBuff;
                        }
                    }
                    else if (i == 2)
                    {
                        statBuff.Value += 6f;
                        buffer[i] = statBuff;
                    }
                }
            }
        }
    }

    public static readonly Dictionary<int, List<List<int>>> MerchantStockMap = new()
    {
        { 1, new List<List<int>> { ParseConfigString(Plugin.FirstMerchantOutputItems), ParseConfigString(Plugin.FirstMerchantOutputAmounts), ParseConfigString(Plugin.FirstMerchantInputItems), ParseConfigString(Plugin.FirstMerchantInputAmounts), ParseConfigString(Plugin.FirstMerchantStockAmounts) } },
        { 2, new List<List<int>> { ParseConfigString(Plugin.SecondMerchantOutputItems), ParseConfigString(Plugin.SecondMerchantOutputAmounts), ParseConfigString(Plugin.SecondMerchantInputItems), ParseConfigString(Plugin.SecondMerchantInputAmounts), ParseConfigString(Plugin.SecondMerchantStockAmounts) } },
        { 3, new List<List<int>> { ParseConfigString(Plugin.ThirdMerchantOutputItems), ParseConfigString(Plugin.ThirdMerchantOutputAmounts), ParseConfigString(Plugin.ThirdMerchantInputItems), ParseConfigString(Plugin.ThirdMerchantInputAmounts), ParseConfigString(Plugin.ThirdMerchantStockAmounts) } },
        { 4, new List<List<int>> { ParseConfigString(Plugin.FourthMerchantOutputItems), ParseConfigString(Plugin.FourthMerchantOutputAmounts), ParseConfigString(Plugin.FourthMerchantInputItems), ParseConfigString(Plugin.FourthMerchantInputAmounts), ParseConfigString(Plugin.FourthMerchantStockAmounts) } },
        { 5, new List<List<int>> { ParseConfigString(Plugin.FifthMerchantOutputItems), ParseConfigString(Plugin.FifthMerchantOutputAmounts), ParseConfigString(Plugin.FifthMerchantInputItems), ParseConfigString(Plugin.FifthMerchantInputAmounts), ParseConfigString(Plugin.FifthMerchantStockAmounts) } },
    };
    public static List<int> ParseConfigString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }

        return configString.Split(',').Select(int.Parse).ToList();
    }

    // shadowmatter weapons
    // Item_Weapon_GreatSword_T09_ShadowMatter PrefabGuid(1322254792) -328302080
    // Item_Weapon_Crossbow_T09_ShadowMatter PrefabGuid(1957540013) -1770479364
    // Item_Weapon_Spear_T09_ShadowMatter PrefabGuid(1307774440) 992015964
    // Item_Weapon_Slashers_T09_ShadowMatter PrefabGuid(506082542) -1965562520
    // Item_Weapon_Whip_T09_ShadowMatter PrefabGuid(567413754) -499397494
    // Item_Weapon_Pistols_T09_ShadowMatter PrefabGuid(-1265586439) 688350142
    // Item_Weapon_Reaper_T09_ShadowMatter PrefabGuid(-465491217) 346834991

    public static readonly List<PrefabGUID> ShadowMatterWeapons =
    [
        new(1322254792),
        new(1307774440)
    ];

    public static readonly Dictionary<PrefabGUID, List<PrefabGUID>> ShadowMatterAbilitiesMap = new()
    {
        { new(1322254792), new List<PrefabGUID> { new(-328302080), new(-2126197617), new(938684260) } },  // GreatSword
        { new(1307774440), new List<PrefabGUID> { new(-1965562520), new(1826128809), new(1730729556) } }  // Spear
    };

    public static readonly Dictionary<PrefabGUID, List<PrefabGUID>> SpellModSets = new()
    {
        { new(2067760264), new() { new(306122420), new(-1068750721), new(-1789930630), new(-946443951), new(681330075), new(-209970409), new(-1612403007), new(2051676361), new(1475152083) } }, // blood fountain
        { new(651613264), new() { new(1612736867), new(-503268826), new(1585822911), new(2035114890), new(2088281423), new(-1565427919), new(-47350874) } }, // blood rage
        { new(1191439206), new() { new(-2135785408), new(361109184), new(395008950), new(-1298328788), new(-1364514258), new(-1514094720), new(864217573), new(291310353) } }, // blood rite
        { new(189403977), new() { new(459492812), new(786676751), new(515468772), new(-1106879810), new(-2026740129), new(2115999081), new(-1721922606), new(-2009288107) } }, // sanguine coil
        { new(-880131926), new() { new(-1144993512), new(411514116), new(-218122346), new(-1967214301), new(-111114882), new(1439297485), new(-1967899075), new(-2009288107) } }, // shadowbolt
        { new(305230608), new() { new(626026650), new(1855739816), new(156877668), new(1384658374), new(255266111), new(-1430581265) } }, // veil of blood
        { new(1575317901), new() { new(-648008702), new(-68573491), new(-960235388), new(2113057383), new(1439297485), new(-1772665607) } }, // aftershock
        { new(-1016145613), new() { new(-547116142), new(-1611128617), new(1906516980), new(1600880528), new(-1251505269), new(1930502023) } }, // chaos barrier
        { new(1112116762), new() { new(23473943), new(10430423), new(-1414823595), new(1749175755), new(-842072895), new(2062624895), new(-581430582), new(-47350874) } }, // power surge
        { new(-358319417), new() { new(281216122), new(-1310320536), new(-2083269917), new(1886458301), new(2113057383), new(681802645) } }, // void
        { new(1019568127), new() { new(1104681306), new(-681348970), new(2113057383), new(1439297485), new(-628722771), new(-2009288107) } }, // chaos volley
        { new(711231628), new() { new(2000559018), new(-593156502), new(-812464660), new(1702103303), new(255266111), new(-1430581265) } }, // veil of chaos
        { new(-1000260252), new() { new(1336836422), new(-1757583318), new(986977415), new(1616797198), new(-311910625), new(1222918506), new(291310353) } }, // cold snap
        { new(295045820), new() { new(-771579655), new(-30104212), new(-111114882), new(-311910625), new(950989548), new(-2009288107) } }, // crystal lance
        { new(1293609465), new() { new(-178978862), new(-581148490), new(631373543), new(1944125102), new(536126279), new(774570130), new(1930502023) } }, // frost barrier
        { new(78384915), new() { new(1644464649), new(440375591), new(-111114882), new(-2047023759), new(950989548), new(-2009288107) } }, // frost bat
        { new(91249849), new() { new(-1070941840), new(-1916056946), new(1934366532), new(1439297485), new(681802645) } }, // ice nova
        { new(1709284795), new() { new(-292495274), new(1126070097), new(-1378154439), new(620700670), new(255266111), new(-1430581265) } }, // veil of frost
        { new(110097606), new() { new(-845453001), new(1301174222), new(-415768376), new(1891772829), new(1552774208), new(291310353), new(-1967899075), new(-1274845133) } }, // mist trance
        { new(268059675), new() { new(-529803606), new(1212582123), new(-1928057811), new(-1673859267), new(-1087850059) } }, // mosquito
        { new(-2053450457), new() { new(928811526), new(-1904117138), new(804206378), new(1484898935), new(-47350874), new(-1967899075), new(-491408666) } }, // phantom aegis
        { new(247896794), new() { new(1531499726), new(1610681142), new(-2009288107), new(1499233761), new(-389780147), new(-1224808007), new(424876885), new(-191364711) } }, // spectral wolf
        { new(-242769430), new() { new(-1565427919), new(1531499726), new(1610681142), new(-1772665607), new(-1653068805), new(-233951066), new(-1538705520) } }, // wraith spear
        { new(-935015750), new() { new(2138408718), new(557219983), new(1016557168), new(-1743623080), new(255266111), new(-1430581265), new(-450361030) } }, // veil of illusion
        { new(1249925269), new() { new(-531481445), new(353305817), new(-485022350), new(-316223882), new(-1772665607), new(292333199) } }, // ball lightning
        { new(-356990326), new() { new(-1643437789), new(2062783787), new(-2009288107), new(1215957974), new(946721895) } }, // cyclone
        { new(1952703098), new() { new(171817139), new(-2071143392), new(98803150), new(1158616225), new(1113225149), new(291310353), new(-1202845465) } }, // discharge
        { new(1071205195), new() { new(1780108774), new(-928750139), new(-635781998), new(-743834336), new(-2109940363) } }, // lightning wall
        { new(-987810170), new() { new(-1565427919), new(-2009288107), new(946721895), new(958439837), new(578859494) } }, // polarity shift
        { new(-84816111), new() { new(1215957974), new(255266111), new(-1430581265), new(-387102419), new(-115293432), new(1221500964) } }, // veil of storm
        { new(481411985), new() { new(585605138), new(-968605931), new(1291379982), new(-612004637), new(47727933), new(419000172), new(1439297485), new(681802645) } }, // bone explosion
        { new(-1204819086), new() { new(1562979558), new(538792139), new(1944307151), new(-203019589), new(-1967899075), new(-2009288107) } }, // corrupted skull
        { new(1961570821), new() { new(406584937), new(771873857), new(1163307889), new(655278112), new(-750244242), new(1830138631) } }, // death knight
        { new(2138402840), new() { new(-696735285), new(-1096014124), new(1670819844), new(-249390913), new(15549217), new(219517192), new(-770033390), new(1871790882) } }, // soulburn
        { new(-1136860480), new() { new(1930502023), new(-1729725919), new(1998410228), new(761541981), new(-649562549), new(909721987), new(-2133606415), new(-1840862497) } }, // ward of the damned
        { new(-498302954), new() { new(-319638993), new(-394612778), new(-1776361271), new(952126692), new(255266111), new(-1430581265) } }, // veil of bones
    };

    public static readonly Dictionary<PrefabGUID, (double Min, double Max)?> SpellModPowerRanges = new()
    {
        // **Blood Fountain Spell Mods**
        { new PrefabGUID(306122420), (14, 25) },          // Eruption impact deals bonus damage.
        { new PrefabGUID(-1068750721), (31, 50) },        // Increases healing of the eruption impact.
        { new PrefabGUID(-1789930630), (19, 30) },        // Increases healing of the initial hit.
        { new PrefabGUID(-946443951), (23, 30) },         // Recast to conjure a lesser blood fountain (initial hit heals).
        { new PrefabGUID(681330075), (29, 40) },          // Eruption hit heals and deals damage.
        { new PrefabGUID(-209970409), null },             // Removes all negative effects from self and allies.
        { new PrefabGUID(-1612403007), (12, 18) },        // Eruption hit increases ally movement speed.
        { new PrefabGUID(2051676361), (2, 3) },           // Eruption hit knocks enemies back (meters).
        { new PrefabGUID(1475152083), null },             // Initial hit inflicts Leech.

        // **Blood Rage Spell Mods**
        { new PrefabGUID(1612736867), (3.8, 6) },         // Increases movement speed.
        { new PrefabGUID(-503268826), (11, 15) },         // Increases physical power during the effect.
        { new PrefabGUID(1585822911), (15, 25) },         // Increases the duration of the effect.
        { new PrefabGUID(2035114890), (0.9, 1.5) },       // Inflicts a fading snare on enemies hit (seconds).
        { new PrefabGUID(2088281423), (1.4, 2.5) },       // Killing an enemy heals you (% of max health).
        { new PrefabGUID(-47350874), (30, 45) },          // Shield self and allies (% of spell power for 3s).

        // **Blood Rite Spell Mods**
        { new PrefabGUID(-2135785408), (4, 5) },          // Throw up to X daggers when triggered.
        { new PrefabGUID(361109184), (25, 40) },          // Each dagger deals damage and inflicts Leech (% damage).
        { new PrefabGUID(395008950), (35, 50) },          // When triggered heals self (% of spell power).
        { new PrefabGUID(-1298328788), (13, 20) },        // Increases damage.
        { new PrefabGUID(-1364514258), (24, 35) },        // Increases movement speed when channeling.
        { new PrefabGUID(-1514094720), (1.8, 2.5) },      // Inflicts a fading snare on enemies hit (seconds).
        { new PrefabGUID(864217573), null },              // Turn invisible during immaterial duration.
        { new PrefabGUID(291310353), (18, 25) },          // Increase immaterial duration (% increase).

        // **Sanguine Coil Spell Mods**
        { new PrefabGUID(459492812), (18, 25) },          // Hitting a target affected by Leech deals bonus damage.
        { new PrefabGUID(786676751), (31, 50) },          // Increases healing received (% of damage dealt).
        { new PrefabGUID(515468772), (29, 40) },          // Bounces to additional target, dealing % of initial damage/healing.
        { new PrefabGUID(-1106879810), null },            // Can bounce back towards caster.
        { new PrefabGUID(-2026740129), null },            // Increase maximum charges by 1.
        { new PrefabGUID(2115999081), null },             // Lethal attacks restore 1 charge.
        { new PrefabGUID(-1721922606), (19, 30) },        // Increases ally healing.

        // **Shadowbolt Spell Mods**
        { new PrefabGUID(-1144993512), (31, 50) },        // Hitting a target affected by Leech deals bonus damage.
        { new PrefabGUID(411514116), (1.9, 3) },          // Hitting a target affected by Leech heals self (% of max health).
        { new PrefabGUID(-218122346), (19, 30) },         // Explodes on hit dealing damage and inflicting Leech.
        { new PrefabGUID(-1967214301), (158, 180) },      // Inflicts Vampiric Curse dealing damage and leeching after 3s.
        { new PrefabGUID(-111114882), (12, 24) },         // Increase projectile range and speed.
        { new PrefabGUID(1439297485), (14, 25) },         // Increases cast rate.
        { new PrefabGUID(-1967899075), (1.9, 3.0) },      // Knocks targets back (meters).
        { new PrefabGUID(-2009288107), (8, 12) },         // Reduces cooldown.

        // **Veil of Blood Spell Mods**
        { new PrefabGUID(626026650), null },              // Dashing through an enemy inflicts Leech.
        { new PrefabGUID(1855739816), (13, 20) },         // Increases elude duration (% increase).
        { new PrefabGUID(156877668), (1.3, 2) },          // Increases healing on hit (% of max health).
        { new PrefabGUID(1384658374), null },             // Next primary attack on an enemy with Leech increases physical damage.
        { new PrefabGUID(255266111), (1.4, 2) },          // Next primary attack inflicts a fading snare (seconds).
        { new PrefabGUID(-1430581265), (14, 25) },        // Next primary attack within 3s deals increased damage.

        // **Aftershock Spell Mods**
        { new PrefabGUID(-648008702), (4, 6) },         // Hitting a target affected by Ignite engulfs the target in Agonising Flames dealing 4-6% damage and healing self.
        { new PrefabGUID(-68573491), (14, 25) },        // Increases damage by 14-25%.
        { new PrefabGUID(-960235388), (15, 25) },       // Increases projectile range by 15-25%.
        { new PrefabGUID(-1772665607), (1.2, 1.8) },    // The initial wave inflicts a fading snare lasting 1.2-1.8s.

        // **Chaos Barrier Spell Mods**
        { new PrefabGUID(-547116142), (0.8, 1.2) },     // A charged chaos bolt stuns target on hit for 0.8-1.2s.
        { new PrefabGUID(-1611128617), (0.5, 0.8) },    // Absorbing an attack reduces cooldown by 0.5-0.8s (up to 3 attacks).
        { new PrefabGUID(1906516980), (31, 50) },       // Explodes on hit dealing 31-50% damage and inflicting Ignite.
        { new PrefabGUID(1600880528), (9, 15) },        // Increases damage per charge by 9-15%.
        { new PrefabGUID(-1251505269), (8, 12) },       // Increases movement speed when channeling by 8-12%.
        { new PrefabGUID(1930502023), (3.3, 4.0) },     // When fully charged, gain a lesser Power Surge lasting 3.3-4s.

        // **Power Surge Spell Mods**
        { new PrefabGUID(23473943), (6, 10) },          // Increases attack speed by 6-10%.
        { new PrefabGUID(10430423), (4, 6) },           // Increases movement speed by 4-6%.
        { new PrefabGUID(-1414823595), (11, 15) },      // Increases physical damage output by 11-15% during the effect.
        { new PrefabGUID(1749175755), (15, 25) },       // Increases the duration of the effect by 15-25%.
        { new PrefabGUID(-842072895), (2, 4) },         // Lethal attacks during the effect reduce the cooldown by 1s (up to 2-4 times).
        { new PrefabGUID(2062624895), (48, 70) },       // Recast to consume the effect, triggering an explosion dealing 48-70% damage and inflicting Ignite.
        { new PrefabGUID(-581430582), null },           // Remove all negative effects.

        // **Void Spell Mods**
        { new PrefabGUID(281216122), (4, 6) },          // Hitting a target affected by Ignite engulfs the target in Agonising Flames dealing 4-6% damage and healing self.
        { new PrefabGUID(-1310320536), (10, 15) },      // Flames engulf the area dealing 10-15% damage up to 3 times.
        { new PrefabGUID(-2083269917), (11, 20) },      // Increases damage by 11-20%.
        { new PrefabGUID(1886458301), (11, 20) },       // Increases range by 11-20%.
        { new PrefabGUID(681802645), (18, 25) },        // Spawns 3 exploding fragments dealing 18-25% damage and inflicting Ignite.

        // **Chaos Volley Spell Mods**
        { new PrefabGUID(1104681306), (4, 6) },         // Hitting a target affected by Ignite engulfs the target in agonizing flames dealing 4-6% damage and healing self.
        { new PrefabGUID(-681348970), (31, 50) },       // Hitting a different target with the second projectile deals 31-50% additional damage.
        { new PrefabGUID(2113057383), (13, 20) },       // Increases damage by 13-20%.
        { new PrefabGUID(-628722771), (0.9, 1.5) },     // Knocks targets back 0.9-1.5 meters on hit.

        // **Veil of Chaos Spell Mods**
        { new PrefabGUID(2000559018), (4, 6) },         // Next primary attack on a target affected by Ignite engulfs the target in Agonising Flames dealing 4-6% damage and healing self.
        { new PrefabGUID(-593156502), (14, 25) },       // Increases damage of the next primary attack within 3s by 14-25%.
        { new PrefabGUID(-812464660), (14, 25) },       // Increases damage done when an illusion explodes by 14-25%.
        { new PrefabGUID(1702103303), (13, 20) },       // Increases elude duration by 13-20%.

        // **Frost Bat Spell Mods**
        { new PrefabGUID(1644464649), (19, 30) },          // Hitting a Chilled or Frozen target shatters the projectile into 8 splinters, each dealing 19-30% damage and inflicting Chill.
        { new PrefabGUID(440375591), (58, 80) },           // Hitting a Chilled or Frozen target shields you for 58-80% of your spell power.
        { new PrefabGUID(-2047023759), (40, 60) },         // The frost impact blast deals 40-60% damage to surrounding enemies.

        // **Ice Nova Spell Mods**
        { new PrefabGUID(-1070941840), (35, 50) },         // Increases damage done to Chilled and Frozen targets by 35-50%.
        { new PrefabGUID(-1916056946), (11, 20) },         // Increases range by 11-20%.
        { new PrefabGUID(1934366532), (31, 50) },          // Recast to conjure a lesser ice nova dealing 31-50% of the original damage.

        // **Cold Snap Spell Mods**
        { new PrefabGUID(1336836422), (13, 20) },          // Increases damage by 13-20%.
        { new PrefabGUID(-1757583318), (0.6, 1.2) },       // Increases Freeze duration by 0.6-1.2s when hitting a Chilled target.
        { new PrefabGUID(986977415), (24, 35) },           // Increases movement speed when channeling by 24-35%.
        { new PrefabGUID(1616797198), (25, 40) },          // The shield absorbs an additional 25-40% of your spell power.
        { new PrefabGUID(-311910625), (11.3, 15) },        // When triggered, increases movement speed by 11.3-15% while the shield lasts.
        { new PrefabGUID(1222918506), (0.8, 1.0) },        // When triggered, turn immaterial for 0.8-1s.

        // **Crystal Lance Spell Mods**
        { new PrefabGUID(-771579655), (19, 30) },          // Hitting a Chilled or Frozen target shatters the projectile into 8 splinters.
        { new PrefabGUID(-30104212), (14, 25) },           // Increases cast rate by 14-25%.
        { new PrefabGUID(950989548), (12, 24) },           // Increases projectile range and speed by 12-24%.

        // **Veil of Frost Spell Mods**
        { new PrefabGUID(-292495274), (25, 40) },          // Illusion explodes in a nova of ice dealing 25-40% damage and inflicting Chill.
        { new PrefabGUID(1126070097), (38, 60) },          // Increase shield absorb amount by 38-60% of your spell power.
        { new PrefabGUID(-1378154439), (14, 25) },         // Increases damage of next primary attack by 14-25%.
        { new PrefabGUID(620700670), (13, 20) },           // Increases elude duration by 13-20%.

        // **Frost Barrier Spell Mods**
        { new PrefabGUID(-178978862), (6, 10) },           // Absorbing an attack increases your spell power by 6-10% for 6s. Effect stacks up to 3 times.
        { new PrefabGUID(-581148490), (0.5, 0.8) },        // Absorbing an attack reduces cooldown by 0.5-0.8s from up to 3 attacks.
        { new PrefabGUID(631373543), (2.5, 4.0) },         // Cone of cold consumes Chill and inflicts Freeze lasting 2.5-4s.
        { new PrefabGUID(1944125102), (1.9, 3.0) },        // Cone of cold knocks targets back 1.9-3 meters on hit.
        { new PrefabGUID(536126279), (58, 80) },           // Hitting a Chilled or Frozen target with the cone of cold shields you for 58-80% of your spell power.
        { new PrefabGUID(774570130), (11, 20) },           // Increases damage by 11-20%.

        // **Spectral Wolf Spell Mods**
        { new PrefabGUID(1531499726), (38, 50) },          // Consumes Weaken to spawn a wisp healing self or ally (% of spell power)
        { new PrefabGUID(1499233761), (1, 2) },            // Increases maximum bounces by
        { new PrefabGUID(-389780147), (12, 24) },          // Increases projectile range and speed by %
        { new PrefabGUID(-1224808007), (1.2, 1.8) },       // Initial projectile inflicts a fading snare lasting seconds
        { new PrefabGUID(424876885), (9, 15) },            // Reduces damage penalty per bounce by %
        { new PrefabGUID(-191364711), (85, 100) },         // The wolf returns to you after the last bounce healing for % of spell power

        // **Wraith Spear Spell Mods**
        { new PrefabGUID(-1565427919), (38, 50) },         // Consumes Weaken to spawn a wisp healing self or ally (% of spell power)
        { new PrefabGUID(1610681142), (25, 40) },          // Hitting a target affected by Weaken grants a shield absorbing % of spell power
        { new PrefabGUID(-1653068805), (14, 25) },         // Increases damage by %
        { new PrefabGUID(-233951066), (15, 25) },          // Increases projectile range by %
        { new PrefabGUID(-1538705520), (0.9, 1.5) },       // Inflicts a fading snare on enemies hit lasting seconds

        // **Phantom Aegis Spell Mods**
        { new PrefabGUID(928811526), (10, 14) },           // Increases movement speed by %
        { new PrefabGUID(-1904117138), (15, 24) },         // Increases target spell power by % while the barrier lasts
        { new PrefabGUID(804206378), (19, 30) },           // Increases the duration of the effect by %
        { new PrefabGUID(1484898935), (1.9, 3.0) },        // Knock targets back meters on hit
        { new PrefabGUID(-491408666), (1.2, 1.6) },        // Barrier explodes if destroyed, inflicting Fear lasting seconds

        // **Mosquito Spell Mods**
        { new PrefabGUID(-529803606), (25, 40) },          // Increases damage by %
        { new PrefabGUID(1212582123), (0.5, 0.8) },        // Increases duration of Fear by seconds
        { new PrefabGUID(-1928057811), (25, 40) },         // Increases mosquito maximum health by %
        { new PrefabGUID(-1673859267), (45, 60) },         // Shields nearby allies for % of spell power when summoned
        { new PrefabGUID(-1087850059), (38, 60) },         // Spawns wisps healing self or ally for % of spell power when the mosquito explodes

        // **Veil of Illusion Spell Mods**
        { new PrefabGUID(2138408718), (24, 35) },          // Detonate your illusion dealing % damage and inflicting Weaken
        { new PrefabGUID(557219983), (25, 40) },           // Hitting a target affected by Weaken grants a shield absorbing % of spell power
        { new PrefabGUID(1016557168), (13, 20) },          // Illusion projectiles deal % damage
        { new PrefabGUID(-1743623080), (14, 25) },         // Increases damage of the next primary attack by %
        { new PrefabGUID(-450361030), (1.4, 2.0) },        // Next primary attack inflicts a fading snare lasting seconds

        // **Mist Trance Spell Mods**
        { new PrefabGUID(-845453001), (11, 20) },          // Increases distance traveled by %
        { new PrefabGUID(1301174222), (24, 35) },          // Increases movement speed when channeling by %
        { new PrefabGUID(-415768376), (1.9, 3.0) },        // Knock targets back meters on hit
        { new PrefabGUID(1891772829), (1.3, 2.0) },        // When triggered fears nearby enemies for seconds
        { new PrefabGUID(1552774208), (2, 4) },            // When triggered grants stacks of Phantasm
        { new PrefabGUID(-1274845133), (31, 50) },         // When triggered next primary attack deals bonus damage by %

        // **Cyclone Spell Mods**
        { new PrefabGUID(-1643437789), (13, 20) },          // Increases damage by 13 - 20%.
        { new PrefabGUID(2062783787), (14, 25) },           // Increases projectile lifetime by 14 - 25%.
        { new PrefabGUID(1215957974), (0.4, 0.5) },         // Consumes Static to stun the target for 0.4 - 0.5s.
        { new PrefabGUID(946721895), (18, 25) },            // Consumes Static to charge your weapon up to 3 times. Next primary attack deals 18 - 25% bonus damage per stack and inflicts Static.

        // **Ball Lightning Spell Mods**
        { new PrefabGUID(-531481445), (4, 6) },             // Increases damage per shock by 4 - 6%.
        { new PrefabGUID(353305817), (40, 80) },            // Recast to detonate the orb early. The explosion deals 40 - 80% increased damage.
        { new PrefabGUID(-485022350), (10, 15) },           // Increases nearby allies' movement speed by 10 - 15% for 4s when the orb explodes.
        { new PrefabGUID(-316223882), (1.9, 3.0) },         // Knocks enemies back 1.9 - 3 meters when the orb explodes.
        { new PrefabGUID(292333199), (0.4, 0.5) },          // Consumes Static to stun enemies for 0.4 - 0.5s when the orb explodes.

        // **Discharge Spell Mods**
        { new PrefabGUID(171817139), (13, 20) },            // Increases damage done by Storm Shields by 13 - 20%.
        { new PrefabGUID(-2071143392), (0.9, 1.2) },        // Turn immaterial for 0.9 - 1.2s if you have 3 Storm Shields active when the effect ends.
        { new PrefabGUID(98803150), (0.2, 0.3) },           // Increases duration of the stun effect by 0.2 - 0.3s.
        { new PrefabGUID(1158616225), (19, 30) },           // Recast to end the effect triggering an explosion dealing 19 - 30% damage and knocking nearby enemies back.
        { new PrefabGUID(1113225149), (4, 6) },             // Each active Storm Shield grants 4 - 6% spell life leech.
        { new PrefabGUID(-1202845465), (18, 25) },          // When triggered, charges your weapon up to 3 times. Next primary attack deals 18 - 25% bonus damage per stack and inflicts Static.

        // **Polarity Shift Spell Mods**
        { new PrefabGUID(958439837), (40, 60) },            // Triggers a lightning nova upon reaching the destination dealing 40 - 60% damage and inflicting Static on nearby enemies.
        { new PrefabGUID(578859494), (40, 60) },            // Triggers a lightning nova at the origin location dealing 40 - 60% damage and inflicting Static on nearby enemies.

        // **Veil of Storm Spell Mods**
        { new PrefabGUID(-387102419), (1.4, 2.0) },         // Next primary attack inflicts a fading snare lasting 1.4 - 2s.
        { new PrefabGUID(-115293432), null },               // Dashing through an enemy inflicts Static.
        { new PrefabGUID(1221500964), (10, 20) },           // Your illusion periodically shocks a nearby enemy dealing 10 - 20% damage and inflicting Static.

        // **Lightning Curtain (Lightning Wall) Spell Mods**
        { new PrefabGUID(1780108774), (30, 50) },           // Passing through the wall shields ally target for 30 - 50% of your spell power.
        { new PrefabGUID(-928750139), (6, 12) },            // Increases damage per hit by 6 - 12%.
        { new PrefabGUID(-635781998), (18, 25) },           // Blocking projectiles charges your weapon up to 3 times. Next primary attack deals 18 - 25% bonus damage per stack and inflicts Static.
        { new PrefabGUID(-743834336), (0.9, 1.5) },         // Inflicts a fading snare lasting 0.9 - 1.5s on the initial hit.
        { new PrefabGUID(-2109940363), (11, 15) },          // Increases movement speed by an additional 11 - 15% when passing through the wall.

        // **Corrupted Skull Spell Mods**
        { new PrefabGUID(1562979558), (9, 15) },            // Conjures a bone spirit that circles around the target dealing 9 - 15% damage and inflicting Condemn
        { new PrefabGUID(538792139), (48, 70) },            // Hitting an ally skeleton causes it to explode dealing 48 - 70% damage and inflicting Condemn
        { new PrefabGUID(1944307151), (11, 20) },           // Increases damage by 11 - 20%
        { new PrefabGUID(-203019589), (12, 24) },           // Increases projectile range and speed by 12 - 24%

        // **Bone Explosion Spell Mods**
        { new PrefabGUID(585605138), (18, 25) },            // Deals 18 - 25% bonus damage to enemies below 30% health, lethal attacks reduce cooldown by 1.7 - 2.3s
        { new PrefabGUID(-968605931), null },               // Heals all allied skeletons hit by 58 - 80% of their maximum health and resets their lifetime
        { new PrefabGUID(1291379982), (13, 20) },           // Increases damage by 13 - 20%
        { new PrefabGUID(-612004637), (11, 20) },           // Increases range by 11 - 20%
        { new PrefabGUID(47727933), (8, 12) },              // Reduces cooldown by 8 - 12%
        { new PrefabGUID(419000172), (1.0, 1.5) },          // Inflicts a fading snare on enemies lasting 1 - 1.5s

        // **Ward of the Damned Spell Mods**
        { new PrefabGUID(-1729725919), (30, 45) },          // Absorbing a projectile heals you for 30 - 45% of your spell power
        { new PrefabGUID(1998410228), (19, 30) },           // Empower allied skeletons hit by the recast increasing their damage output by 19 - 30% for 8s
        { new PrefabGUID(761541981), (13, 20) },            // Increases damage done by the recast attack by 13 - 20%
        { new PrefabGUID(-649562549), (8, 12) },            // Increases movement speed when channeling by 8 - 12%
        { new PrefabGUID(909721987), (31, 50) },            // Melee attackers take 31 - 50% damage when striking the barrier
        { new PrefabGUID(-2133606415), (75, 120) },         // Shields allied skeletons hit by the recast for 75 - 120% of your spell power, lasts for 4s
        { new PrefabGUID(-1840862497), (1.9, 3.0) },        // The recast attack knocks targets back 1.9 - 3.0 meters on hit

        // **Death Knight Spell Mods**
        { new PrefabGUID(406584937), (25, 40) },            // Summon a Skeleton Mage with 25 - 40% increased power when the Death Knight lifetime expires
        { new PrefabGUID(771873857), (18, 25) },            // Damage done by the Death Knight heals you for 18 - 25% of damage done
        { new PrefabGUID(1163307889), (1.2, 1.7) },         // Inflicts a fading snare lasting 1.2 - 1.7s on enemies nearby the summoning location
        { new PrefabGUID(655278112), (14, 20) },            // Increases damage by 14 - 20%
        { new PrefabGUID(-750244242), (25, 40) },           // Increases Death Knight lifetime by 25 - 40%
        { new PrefabGUID(1830138631), (25, 40) },           // Increases Death Knight health by 25 - 40%

        // **Veil of Bones Spell Mods**
        { new PrefabGUID(-319638993), null },               // Dashing through an enemy inflicts Condemn
        { new PrefabGUID(-394612778), null },               // Heals all allied skeletons you dash through by 58 - 80% of their maximum health and resets their lifetime
        { new PrefabGUID(-1776361271), (13, 20) },          // Increases elude duration by 13 - 20%
        { new PrefabGUID(952126692), (14, 25) },            // Increases damage of next primary attack within 3s by 14 - 25%

        // **Soulburn Spell Mods**
        { new PrefabGUID(-696735285), (50, 80) },           // Consume up to 3 allied skeletons healing you for 50 - 80% of your spell power per skeleton
        { new PrefabGUID(-1096014124), (7, 12) },           // Consume up to 3 allied skeletons increasing your spell and physical power by 7 - 12% per skeleton for 8s
        { new PrefabGUID(1670819844), (10, 16) },           // Increases damage by 10 - 16%
        { new PrefabGUID(-249390913), (8, 15) },            // Increases life drain by 8 - 15%
        { new PrefabGUID(15549217), (0.4, 0.5) },           // Increases silence duration by 0.4 - 0.5s
        { new PrefabGUID(219517192), (1, 1) },              // Increases targets hit by 1
        { new PrefabGUID(-770033390), (0.5, 0.8) },         // Reduces cooldown by 0.5 - 0.8s for each target silenced
        { new PrefabGUID(1871790882), null },               // Removes all negative effects from self on cast
    };

    /*
    public static readonly List<PrefabGUID> statModSets =
    [
        new(-542568600),
        new(-184681371),
        new(-1480767601),
        new(-1157374165),
        new(193642528),
        new(303731846),
        new(-1644092685),
        new(1915954443),
        new(-285192213),
        new(-1917650844),
        new(-1276596814),
        new(-1639076208),
        new(-427223401),
        new(1705753146),
        new(-1122907647),
        new(-1545133628),
        new(1448170922),
        new(-1700712765),
        new(523084427),
        new(1179205309),
        new(-1274939577),
        new(1032018140),
        new(-2004879548),
        new(539854831),
        new(-269007548),
        new(-1466424600),
        new(1842448780),
        new(-1282352396),
        new(369266120),
        new(1732724221),
        new(1410628524),
        new(-1088278970),
        new(-20933838),
        new(-394348256),
        new(-1171110486),
        new(-1659606994),
        new(-1122907647)
    ];
    */
}




