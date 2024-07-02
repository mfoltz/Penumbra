using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Merchants.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
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
    public static ServerGameSettingsSystem ServerGameSettingsSystem { get; internal set; }
    public static ServerScriptMapper ServerScriptMapper { get; internal set; }
    public static DebugEventsSystem DebugEventsSystem { get; internal set; }
    public static EntityCommandBufferSystem EntityCommandBufferSystem { get; internal set; }
    public static GameDataSystem GameDataSystem { get; internal set; }
    public static ServerGameManager ServerGameManager => ServerScriptMapper.GetServerGameManager();
    public static NetworkIdSystem.Singleton NetworkIdSystem { get; internal set; }
    public static ScriptSpawnServer ScriptSpawnServer { get; internal set;}
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
        GameDataSystem = Server.GetExistingSystemManaged<GameDataSystem>();
        NetworkIdSystem = ServerScriptMapper.GetSingleton<NetworkIdSystem.Singleton>();
        ScriptSpawnServer = Server.GetExistingSystemManaged<ScriptSpawnServer>();
        ServerGameSettings = ServerGameSettingsSystem._Settings;

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
        { 5, new List<List<int>> { ParseConfigString(Plugin.FifthMerchantOutputItems), ParseConfigString(Plugin.FifthMerchantOutputAmounts), ParseConfigString(Plugin.FifthMerchantInputItems), ParseConfigString(Plugin.FifthMerchantInputAmounts), ParseConfigString(Plugin.FifthMerchantStockAmounts) } }
    };
    static List<int> ParseConfigString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }
        return configString.Split(',').Select(int.Parse).ToList();
    }  
}




