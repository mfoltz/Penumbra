using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using VampireCommandFramework;

namespace Merchants;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;

    public static readonly string ConfigFiles = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME); // Merchants folder

    // current paths
    public static readonly string MerchantConfigPath = Path.Combine(ConfigFiles, "Merchants");

    // config entries
    private static ConfigEntry<int> _restockTime; // restock time in minutes

    private static ConfigEntry<string> _firstMerchantOutputItems; // item prefabs for output
    private static ConfigEntry<string> _firstMerchantOutputAmounts; // item amounts for output
    private static ConfigEntry<string> _firstMerchantInputItems; // item prefabs for input
    private static ConfigEntry<string> _firstMerchantInputAmounts; // item amounts for input
    private static ConfigEntry<string> _firstMerchantStockAmounts; // stock amounts for outputs

    private static ConfigEntry<string> _secondMerchantOutputItems;
    private static ConfigEntry<string> _secondMerchantOutputAmounts;
    private static ConfigEntry<string> _secondMerchantInputItems;
    private static ConfigEntry<string> _secondMerchantInputAmounts;
    private static ConfigEntry<string> _secondMerchantStockAmounts;

    private static ConfigEntry<string> _thirdMerchantOutputItems;
    private static ConfigEntry<string> _thirdMerchantOutputAmounts;
    private static ConfigEntry<string> _thirdMerchantInputItems;
    private static ConfigEntry<string> _thirdMerchantInputAmounts;
    private static ConfigEntry<string> _thirdMerchantStockAmounts;

    private static ConfigEntry<string> _fourthMerchantOutputItems;
    private static ConfigEntry<string> _fourthMerchantOutputAmounts;
    private static ConfigEntry<string> _fourthMerchantInputItems;
    private static ConfigEntry<string> _fourthMerchantInputAmounts;
    private static ConfigEntry<string> _fourthMerchantStockAmounts;

    private static ConfigEntry<string> _fifthMerchantOutputItems;
    private static ConfigEntry<string> _fifthMerchantOutputAmounts;
    private static ConfigEntry<string> _fifthMerchantInputItems;
    private static ConfigEntry<string> _fifthMerchantInputAmounts;
    private static ConfigEntry<string> _fifthMerchantStockAmounts;

    public static int RestockTime => _restockTime.Value;
    public static string FirstMerchantOutputItems => _firstMerchantOutputItems.Value;
    public static string FirstMerchantOutputAmounts => _firstMerchantOutputAmounts.Value;
    public static string FirstMerchantInputItems => _firstMerchantInputItems.Value;
    public static string FirstMerchantInputAmounts => _firstMerchantInputAmounts.Value;
    public static string FirstMerchantStockAmounts => _firstMerchantStockAmounts.Value;

    public static string SecondMerchantOutputItems => _secondMerchantOutputItems.Value;
    public static string SecondMerchantOutputAmounts => _secondMerchantOutputAmounts.Value;
    public static string SecondMerchantInputItems => _secondMerchantInputItems.Value;
    public static string SecondMerchantInputAmounts => _secondMerchantInputAmounts.Value;
    public static string SecondMerchantStockAmounts => _secondMerchantStockAmounts.Value;

    public static string ThirdMerchantOutputItems => _thirdMerchantOutputItems.Value;
    public static string ThirdMerchantOutputAmounts => _thirdMerchantOutputAmounts.Value;
    public static string ThirdMerchantInputItems => _thirdMerchantInputItems.Value;
    public static string ThirdMerchantInputAmounts => _thirdMerchantInputAmounts.Value;
    public static string ThirdMerchantStockAmounts => _thirdMerchantStockAmounts.Value;

    public static string FourthMerchantOutputItems => _fourthMerchantOutputItems.Value;
    public static string FourthMerchantOutputAmounts => _fourthMerchantOutputAmounts.Value;
    public static string FourthMerchantInputItems => _fourthMerchantInputItems.Value;
    public static string FourthMerchantInputAmounts => _fourthMerchantInputAmounts.Value;
    public static string FourthMerchantStockAmounts => _fourthMerchantStockAmounts.Value;

    public static string FifthMerchantOutputItems => _fifthMerchantOutputItems.Value;
    public static string FifthMerchantOutputAmounts => _fifthMerchantOutputAmounts.Value;
    public static string FifthMerchantInputItems => _fifthMerchantInputItems.Value;
    public static string FifthMerchantInputAmounts => _fifthMerchantInputAmounts.Value;
    public static string FifthMerchantStockAmounts => _fifthMerchantStockAmounts.Value;
    public override void Load()
    {
        Instance = this;
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        CommandRegistry.RegisterAll();
        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded!");
    }
    static void InitConfig()
    {
        CreateDirectories(ConfigFiles);
        _restockTime = InitConfigEntry("General", "RestockTime", 5, "The restock time in minutes for merchants.");
        _firstMerchantOutputItems = InitConfigEntry("FirstMerchant", "OutputItems", "", "The item prefabs for the first merchant's output.");
        _firstMerchantOutputAmounts = InitConfigEntry("FirstMerchant", "OutputAmounts", "", "The item amounts for the first merchant's output.");
        _firstMerchantInputItems = InitConfigEntry("FirstMerchant", "InputItems", "", "The item prefabs for the first merchant's input.");
        _firstMerchantInputAmounts = InitConfigEntry("FirstMerchant", "InputAmounts", "", "The item amounts for the first merchant's input.");
        _firstMerchantStockAmounts = InitConfigEntry("FirstMerchant", "StockAmounts", "", "The stock amounts for the first merchant's outputs.");
        _secondMerchantOutputItems = InitConfigEntry("SecondMerchant", "OutputItems", "", "The item prefabs for the second merchant's output.");
        _secondMerchantOutputAmounts = InitConfigEntry("SecondMerchant", "OutputAmounts", "", "The item amounts for the second merchant's output.");
        _secondMerchantInputItems = InitConfigEntry("SecondMerchant", "InputItems", "", "The item prefabs for the second merchant's input.");
        _secondMerchantInputAmounts = InitConfigEntry("SecondMerchant", "InputAmounts", "", "The item amounts for the second merchant's input.");
        _secondMerchantStockAmounts = InitConfigEntry("SecondMerchant", "StockAmounts", "", "The stock amounts for the second merchant's outputs.");
        _thirdMerchantOutputItems = InitConfigEntry("ThirdMerchant", "OutputItems", "", "The item prefabs for the third merchant's output.");
        _thirdMerchantOutputAmounts = InitConfigEntry("ThirdMerchant", "OutputAmounts", "", "The item amounts for the third merchant's output.");
        _thirdMerchantInputItems = InitConfigEntry("ThirdMerchant", "InputItems", "", "The item prefabs for the third merchant's input.");
        _thirdMerchantInputAmounts = InitConfigEntry("ThirdMerchant", "InputAmounts", "", "The item amounts for the third merchant's input.");
        _thirdMerchantStockAmounts = InitConfigEntry("ThirdMerchant", "StockAmounts", "", "The stock amounts for the third merchant's outputs.");
        _fourthMerchantOutputItems = InitConfigEntry("FourthMerchant", "OutputItems", "", "The item prefabs for the fourth merchant's output.");
        _fourthMerchantOutputAmounts = InitConfigEntry("FourthMerchant", "OutputAmounts", "", "The item amounts for the fourth merchant's output.");
        _fourthMerchantInputItems = InitConfigEntry("FourthMerchant", "InputItems", "", "The item prefabs for the fourth merchant's input.");
        _fourthMerchantInputAmounts = InitConfigEntry("FourthMerchant", "InputAmounts", "", "The item amounts for the fourth merchant's input.");
        _fourthMerchantStockAmounts = InitConfigEntry("FourthMerchant", "StockAmounts", "", "The stock amounts for the fourth merchant's outputs.");
        _fifthMerchantOutputItems = InitConfigEntry("FifthMerchant", "OutputItems", "", "The item prefabs for the fifth merchant's output.");
        _fifthMerchantOutputAmounts = InitConfigEntry("FifthMerchant", "OutputAmounts", "", "The item amounts for the fifth merchant's output.");
        _fifthMerchantInputItems = InitConfigEntry("FifthMerchant", "InputItems", "", "The item prefabs for the fifth merchant's input.");
        _fifthMerchantInputAmounts = InitConfigEntry("FifthMerchant", "InputAmounts", "", "The item amounts for the fifth merchant's input.");
        _fifthMerchantStockAmounts = InitConfigEntry("FifthMerchant", "StockAmounts", "", "The stock amounts for the fifth merchant's outputs.");
    }

    static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        // Bind the configuration entry and get its value
        var entry = Instance.Config.Bind(section, key, defaultValue, description);

        // Check if the key exists in the configuration file and retrieve its current value
        var newFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(newFile))
        {
            var config = new ConfigFile(newFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                // If the entry exists, update the value to the existing value
                entry.Value = existingEntry.Value;
            }
        }

        return entry;
    }
    static void CreateDirectories(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public override bool Unload()
    {
        Config.Clear();
        _harmony.UnpatchSelf();
        return true;
    }
}