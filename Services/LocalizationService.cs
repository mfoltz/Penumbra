using Stunlock.Core;
using System.Reflection;
using System.Text.Json;
using Unity.Collections;
using static Penumbra.Resources.PrefabNames;

namespace Penumbra.Services;
internal class LocalizationService // the bones are from KindredCommands, ty Odjit c:
{
    struct Code
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }
    struct Node
    {
        public string Guid { get; set; }
        public string Text { get; set; }
    }
    struct Word
    {
        public string Original { get; set; }
        public string Translation { get; set; }
    }
    struct LocalizationFile
    {
        public Code[] Codes { get; set; }
        public Node[] Nodes { get; set; }
        public Word[] Words { get; set; }
    }

    static readonly Dictionary<int, string> _guidHashesToGuidStrings = [];
    static readonly Dictionary<string, string> _guidStringsToLocalizedNames = [];
    public static IReadOnlyDictionary<PrefabGUID, string> PrefabGuidNames => _prefabGuidNames;
    static readonly Dictionary<PrefabGUID, string> _prefabGuidNames = [];
    public LocalizationService()
    {
        InitializeLocalizations();
        InitializePrefabGuidNames();
    }
    static void InitializeLocalizations()
    {
        LoadGuidStringsToLocalizedNames();
    }
    static void InitializePrefabGuidNames()
    {
        var namesToPrefabGuids = Core.PrefabCollectionSystem._PrefabDataLookup;

        var prefabGuids = namesToPrefabGuids.GetKeyArray(Allocator.Temp);
        var assetData = namesToPrefabGuids.GetValueArray(Allocator.Temp);

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                var prefabGuid = prefabGuids[i];
                var assetDataValue = assetData[i];

                _prefabGuidNames[prefabGuid] = assetDataValue.AssetName.Value;
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Error initializing prefab names: {ex.Message}");
        }
        finally
        {
            // Core.Log.LogWarning($"[LocalizationService] Prefab names initialized - {_prefabGuidsToNames.Count}");
            prefabGuids.Dispose();
            assetData.Dispose();
        }
    }
    static void LoadGuidStringsToLocalizedNames()
    {
        string resourceName = "Penumbra.Resources.Localization.English.json";
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Plugin.LogInstance.LogInfo($"[Localization] Trying to load resource - {resourceName}");

        Stream stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            Plugin.LogInstance.LogError($"[Localization] Failed to load resource - {resourceName}");
        }

        using StreamReader localizationReader = new(stream);

        string jsonContent = localizationReader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            Plugin.LogInstance.LogError($"[Localization] No JSON content!");
        }

        var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (localizationFile.Nodes == null)
        {
            Plugin.LogInstance.LogError($"[Localization] Deserialized file is null or missing Nodes!");
        }

        localizationFile.Nodes
            .ToDictionary(x => x.Guid, x => x.Text)
            .ForEach(kvp => _guidStringsToLocalizedNames[kvp.Key] = kvp.Value);
    }
    public static string GetAssetGuidString(PrefabGUID prefabGUID)
    {
        if (_guidHashesToGuidStrings.TryGetValue(prefabGUID.GuidHash, out var guidString))
        {
            return guidString;
        }

        return string.Empty;
    }
    public static string GetGuidString(PrefabGUID prefabGuid)
    {
        if (LocalizedNameKeys.TryGetValue(prefabGuid, out string guidString))
        {
            return guidString;
        }

        return string.Empty;
    }
    public static string GetNameFromGuidString(string guidString)
    {
        if (_guidStringsToLocalizedNames.TryGetValue(guidString, out string localizedName))
        {
            return localizedName;
        }

        return string.Empty;
    }
    public static string GetNameFromPrefabGuid(PrefabGUID prefabGuid)
    {
        return GetNameFromGuidString(GetGuidString(prefabGuid));
    }
}
