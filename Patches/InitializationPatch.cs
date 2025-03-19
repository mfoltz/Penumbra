using HarmonyLib;
using Unity.Scenes;

namespace Penumbra.Patches;

[HarmonyPatch]
internal static class InitializationPatch
{
    [HarmonyPatch(typeof(SceneSystem), nameof(SceneSystem.ShutdownStreamingSupport))]
    [HarmonyPostfix]
    static void ShutdownStreamingSupportPostfix()
    {
        try
        {
            Core.Initialize();

            if (Core._hasInitialized)
            {
                Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized!");
                Plugin.Harmony.Unpatch(typeof(SceneSystem).GetMethod("ShutdownStreamingSupport"), typeof(InitializationPatch).GetMethod("ShutdownStreamingSupportPostfix"));
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to initialize, exiting on try-catch - {ex}");
        }
    }
}