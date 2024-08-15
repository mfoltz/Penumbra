using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Merchants.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Merchants;
internal static class Core
{
    public static World Server { get; } = GetWorld("Server") ?? throw new Exception("There is no Server world (yet)...");
    public static EntityManager EntityManager { get; } = Server.EntityManager;
    public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }
    public static BehaviourTreeBindingSystem_Spawn BehaviourTreeBindingSystem_Spawn { get; internal set; }

    //public static SpellModCollectionSystem SpellModCollectionSystem { get; internal set; }
    public static JewelSpawnSystem JewelSpawnSystem { get; internal set; }
    public static SpellModSpawnSystem SpellModSpawnSystem { get; internal set; }
    public static SpellModSyncPersistenceSystem SpellModSyncPersistenceSystem { get; internal set; }
    public static SpellModSyncSystem_Server SpellModSyncSystem_Server { get; internal set; }
    public static ServerGameSettingsSystem ServerGameSettingsSystem { get; internal set; }
    public static ServerScriptMapper ServerScriptMapper { get; internal set; }
    public static DebugEventsSystem DebugEventsSystem { get; internal set; }
    public static EntityCommandBufferSystem EntityCommandBufferSystem { get; internal set; }
    public static GameDataSystem GameDataSystem { get; internal set; }
    public static ServerGameManager ServerGameManager => ServerScriptMapper.GetServerGameManager();
    public static NetworkIdSystem.Singleton NetworkIdSystem { get; internal set; }
    public static ScriptSpawnServer ScriptSpawnServer { get; internal set; }
    public static ServerGameSettings ServerGameSettings { get; internal set; }
    public static MerchantService MerchantService { get; } = new();
    public static double ServerTime => ServerGameManager.ServerTime;
    public static ManualLogSource Log => Plugin.LogInstance;

    static MonoBehaviour monoBehaviour;

    public static bool hasInitialized;
    public static void Initialize()
    {
        if (hasInitialized) return;
        // Initialize utility services
        PrefabCollectionSystem = Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        ServerGameSettingsSystem = Server.GetExistingSystemManaged<ServerGameSettingsSystem>();
        DebugEventsSystem = Server.GetExistingSystemManaged<DebugEventsSystem>();
        ServerScriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();
        EntityCommandBufferSystem = Server.GetExistingSystemManaged<EntityCommandBufferSystem>();
        BehaviourTreeBindingSystem_Spawn = Server.GetExistingSystemManaged<BehaviourTreeBindingSystem_Spawn>();
        //SpellModCollectionSystem = Server.GetExistingSystemManaged<SpellModCollectionSystem>();
        JewelSpawnSystem = Server.GetExistingSystemManaged<JewelSpawnSystem>();
        SpellModSpawnSystem = Server.GetExistingSystemManaged<SpellModSpawnSystem>();
        SpellModSyncPersistenceSystem = Server.GetExistingSystemManaged<SpellModSyncPersistenceSystem>();
        SpellModSyncSystem_Server = Server.GetExistingSystemManaged<SpellModSyncSystem_Server>();
        GameDataSystem = Server.GetExistingSystemManaged<GameDataSystem>();
        NetworkIdSystem = ServerScriptMapper.GetSingleton<NetworkIdSystem.Singleton>();
        ScriptSpawnServer = Server.GetExistingSystemManaged<ScriptSpawnServer>();
        ServerGameSettings = ServerGameSettingsSystem._Settings;
        ModifyRelicBuildings();
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
            var go = new GameObject("Merchants");
            monoBehaviour = go.AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
        monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }

    public static Dictionary<int, List<List<int>>> MerchantMap = new()
    {
        { 1, new List<List<int>> { ParseConfigString(Plugin.FirstMerchantOutputItems), ParseConfigString(Plugin.FirstMerchantOutputAmounts), ParseConfigString(Plugin.FirstMerchantInputItems), ParseConfigString(Plugin.FirstMerchantInputAmounts), ParseConfigString(Plugin.FirstMerchantStockAmounts) } },
        { 2, new List<List<int>> { ParseConfigString(Plugin.SecondMerchantOutputItems), ParseConfigString(Plugin.SecondMerchantOutputAmounts), ParseConfigString(Plugin.SecondMerchantInputItems), ParseConfigString(Plugin.SecondMerchantInputAmounts), ParseConfigString(Plugin.SecondMerchantStockAmounts) } },
        { 3, new List<List<int>> { ParseConfigString(Plugin.ThirdMerchantOutputItems), ParseConfigString(Plugin.ThirdMerchantOutputAmounts), ParseConfigString(Plugin.ThirdMerchantInputItems), ParseConfigString(Plugin.ThirdMerchantInputAmounts), ParseConfigString(Plugin.ThirdMerchantStockAmounts) } },
        { 4, new List<List<int>> { ParseConfigString(Plugin.FourthMerchantOutputItems), ParseConfigString(Plugin.FourthMerchantOutputAmounts), ParseConfigString(Plugin.FourthMerchantInputItems), ParseConfigString(Plugin.FourthMerchantInputAmounts), ParseConfigString(Plugin.FourthMerchantStockAmounts) } },
        { 5, new List<List<int>> { ParseConfigString(Plugin.FifthMerchantOutputItems), ParseConfigString(Plugin.FifthMerchantOutputAmounts), ParseConfigString(Plugin.FifthMerchantInputItems), ParseConfigString(Plugin.FifthMerchantInputAmounts), ParseConfigString(Plugin.FifthMerchantStockAmounts) } },
        { 6, new List<List<int>> { ParseConfigString(Plugin.SixthMerchantOutputItems), ParseConfigString(Plugin.SixthMerchantOutputAmounts), ParseConfigString(Plugin.SixthMerchantInputItems), ParseConfigString(Plugin.SixthMerchantInputAmounts), ParseConfigString(Plugin.SixthMerchantStockAmounts) } },
        { 7, new List<List<int>> { ParseConfigString(Plugin.SeventhMerchantOutputItems), ParseConfigString(Plugin.SeventhMerchantOutputAmounts), ParseConfigString(Plugin.SeventhMerchantInputItems), ParseConfigString(Plugin.SeventhMerchantInputAmounts), ParseConfigString(Plugin.SeventhMerchantStockAmounts) } }
    };

    /*
    public static Dictionary<int, int> RestockTimeMap = new()
    {
        { 1, Plugin.FirstMerchantRestockTime },
        { 2, Plugin.SecondMerchantRestockTime },
        { 3, Plugin.ThirdMerchantRestockTime },
        { 4, Plugin.FourthMerchantRestockTime },
        { 5, Plugin.FifthMerchantRestockTime }
    };
    */
    static List<int> ParseConfigString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }
        return configString.Split(',').Select(int.Parse).ToList();
    }
    static void ModifyRelicBuildings()
    {
        var itemMap = Core.GameDataSystem.ItemHashLookupMap;
        foreach(PrefabGUID prefab in RelicBuildings)
        {
            Entity prefabEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];
            ItemData itemData = prefabEntity.Read<ItemData>();
            itemData.ItemCategory &= ~ItemCategory.Relic;
            prefabEntity.Write(itemData);

            itemMap[prefab] = itemData;
        }

        PrefabGUID silverIngot = new(-1787563914);
        Entity silverIngotEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[silverIngot];
        ItemData silverIngotData = silverIngotEntity.Read<ItemData>();
        silverIngotData.SilverValue = 0f;
        silverIngotEntity.Write(silverIngotData);
        itemMap[silverIngot] = silverIngotData;

        PrefabGUID greaterStygianRecipe = new(830427227);
        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[greaterStygianRecipe];
        var requirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        RecipeRequirementBuffer item = requirementBuffer[0];
        item.Amount = 8;
        requirementBuffer[0] = item;
        GameDataSystem.RegisterRecipes();
    }

    static readonly List<PrefabGUID> RelicBuildings =
    [
        new(2019195024),
        new(-1619308732),
        new(-222860772),
        new(1247086852)
    ];

    public static readonly Dictionary<PrefabGUID, List<PrefabGUID>> spellModSets = new()
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




