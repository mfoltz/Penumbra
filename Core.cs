using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Penumbra.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using static Penumbra.Plugin;

namespace Penumbra;
internal static class Core
{
    public static World Server { get; } = GetServerWorld() ?? throw new Exception("There is no Server world!");
    public static EntityManager EntityManager => Server.EntityManager;
    public static ServerGameManager ServerGameManager => ServerScriptMapper.GetServerGameManager();
    public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }
    public static ServerGameSettingsSystem ServerGameSettingsSystem { get; internal set; }
    public static ServerScriptMapper ServerScriptMapper { get; internal set; }
    public static DebugEventsSystem DebugEventsSystem { get; internal set; }
    public static EntityCommandBufferSystem EntityCommandBufferSystem { get; internal set; }
    public static NetworkIdSystem.Singleton NetworkIdSystem { get; internal set; }
    public static double ServerTime => ServerGameManager.ServerTime;
    public static ManualLogSource Log => LogInstance;

    static MonoBehaviour _monoBehaviour;
    public static IReadOnlyDictionary<PrefabGUID, string> PrefabGuidsToNames => _prefabGuidsToNames;
    static readonly Dictionary<PrefabGUID, string> _prefabGuidsToNames = [];

    public static bool _initialized;
    public static void Initialize()
    {
        if (_initialized) return;

        PrefabCollectionSystem = Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        ServerGameSettingsSystem = Server.GetExistingSystemManaged<ServerGameSettingsSystem>();
        DebugEventsSystem = Server.GetExistingSystemManaged<DebugEventsSystem>();
        ServerScriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();
        EntityCommandBufferSystem = Server.GetExistingSystemManaged<EntityCommandBufferSystem>();
        NetworkIdSystem = ServerScriptMapper.GetSingleton<NetworkIdSystem.Singleton>();

        InitializePrefabGuidNames();

        if (_tokensConfig.TokenSystem || _tokensConfig.DailyLogin)
            TokenService.Initialize();

        _ = new LocalizationService();
        _ = new MerchantService();

        _initialized = true;
    }
    static World GetServerWorld()
    {
        return World.s_AllWorlds.ToArray().FirstOrDefault(world => world.Name == "Server");
    }
    static void InitializePrefabGuidNames()
    {
        var namesToPrefabGuids = PrefabCollectionSystem.SpawnableNameToPrefabGuidDictionary;

        foreach (var kvp in namesToPrefabGuids)
        {
            _prefabGuidsToNames[kvp.Value] = kvp.Key;
        }
    }
    public static void StartCoroutine(IEnumerator routine)
    {
        if (_monoBehaviour == null)
        {
            var go = new GameObject(MyPluginInfo.PLUGIN_NAME);
            _monoBehaviour = go.AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        _monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
}




