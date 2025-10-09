using HarmonyLib;
using ProjectM.Gameplay.WarEvents;

namespace Penumbra.Patches;

[HarmonyPatch]
internal static class InitializationPatch
{
    [HarmonyPatch(typeof(WarEventRegistrySystem), nameof(WarEventRegistrySystem.RegisterWarEventEntities))]
    [HarmonyPostfix]
    static void RegisterWarEventEntitiesPostfix()
    {
        try
        {
            Core.Initialize();

            if (Core._initialized)
            {
                Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized!");
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to initialize, exiting on try-catch: {ex}");
        }
    }
}