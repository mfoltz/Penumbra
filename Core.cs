using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime;
using Penumbra.Service;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using System.Collections;
using Unity.Entities;
using UnityEngine;

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
    public static ManualLogSource Log => Plugin.LogInstance;

    static MonoBehaviour _monoBehaviour;

    public static bool _hasInitialized;
    public static void Initialize()
    {
        if (_hasInitialized) return;

        PrefabCollectionSystem = Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        ServerGameSettingsSystem = Server.GetExistingSystemManaged<ServerGameSettingsSystem>();
        DebugEventsSystem = Server.GetExistingSystemManaged<DebugEventsSystem>();
        ServerScriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();
        EntityCommandBufferSystem = Server.GetExistingSystemManaged<EntityCommandBufferSystem>();
        NetworkIdSystem = ServerScriptMapper.GetSingleton<NetworkIdSystem.Singleton>();

        _ = new MerchantService();

        _hasInitialized = true;
    }
    static World GetServerWorld()
    {
        return World.s_AllWorlds.ToArray().FirstOrDefault(world => world.Name == "Server");
    }
    public static void StartCoroutine(IEnumerator routine)
    {
        if (_monoBehaviour == null)
        {
            var go = new GameObject("Penumbra");
            _monoBehaviour = go.AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        _monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
    public static void LogEntity(World world, Entity entity)
    {
        Il2CppSystem.Text.StringBuilder sb = new();

        try
        {
            EntityDebuggingUtility.DumpEntity(world, entity, true, sb);
            Log.LogInfo($"Entity Dump:\n{sb.ToString()}");
        }
        catch (Exception e)
        {
            Log.LogWarning($"Error dumping entity: {e.Message}");
        }
    }
}




