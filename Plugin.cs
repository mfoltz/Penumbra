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
    private static ConfigEntry<int> _restockTime; // restock time in minutes, will be used unless specific restock times are set

    private static ConfigEntry<string> _firstMerchantOutputItems; // item prefabs for output
    private static ConfigEntry<string> _firstMerchantOutputAmounts; // item amounts for output
    private static ConfigEntry<string> _firstMerchantInputItems; // item prefabs for input
    private static ConfigEntry<string> _firstMerchantInputAmounts; // item amounts for input
    private static ConfigEntry<string> _firstMerchantStockAmounts; // stock amounts for outputs
    private static ConfigEntry<int> _firstMerchantRestockTime; // restock time for this merchant

    private static ConfigEntry<string> _secondMerchantOutputItems;
    private static ConfigEntry<string> _secondMerchantOutputAmounts;
    private static ConfigEntry<string> _secondMerchantInputItems;
    private static ConfigEntry<string> _secondMerchantInputAmounts;
    private static ConfigEntry<string> _secondMerchantStockAmounts;
    private static ConfigEntry<int> _secondMerchantRestockTime;

    private static ConfigEntry<string> _thirdMerchantOutputItems;
    private static ConfigEntry<string> _thirdMerchantOutputAmounts;
    private static ConfigEntry<string> _thirdMerchantInputItems;
    private static ConfigEntry<string> _thirdMerchantInputAmounts;
    private static ConfigEntry<string> _thirdMerchantStockAmounts;
    private static ConfigEntry<int> _thirdMerchantRestockTime;

    private static ConfigEntry<string> _fourthMerchantOutputItems;
    private static ConfigEntry<string> _fourthMerchantOutputAmounts;
    private static ConfigEntry<string> _fourthMerchantInputItems;
    private static ConfigEntry<string> _fourthMerchantInputAmounts;
    private static ConfigEntry<string> _fourthMerchantStockAmounts;
    private static ConfigEntry<int> _fourthMerchantRestockTime;

    private static ConfigEntry<string> _fifthMerchantOutputItems;
    private static ConfigEntry<string> _fifthMerchantOutputAmounts;
    private static ConfigEntry<string> _fifthMerchantInputItems;
    private static ConfigEntry<string> _fifthMerchantInputAmounts;
    private static ConfigEntry<string> _fifthMerchantStockAmounts;
    private static ConfigEntry<int> _fifthMerchantRestockTime;

    private static ConfigEntry<string> _sixthMerchantOutputItems;
    private static ConfigEntry<string> _sixthMerchantOutputAmounts;
    private static ConfigEntry<string> _sixthMerchantInputItems;
    private static ConfigEntry<string> _sixthMerchantInputAmounts;
    private static ConfigEntry<string> _sixthMerchantStockAmounts;
    private static ConfigEntry<int> _sixthMerchantRestockTime;

    private static ConfigEntry<string> _seventhMerchantOutputItems;
    private static ConfigEntry<string> _seventhMerchantOutputAmounts;
    private static ConfigEntry<string> _seventhMerchantInputItems;
    private static ConfigEntry<string> _seventhMerchantInputAmounts;
    private static ConfigEntry<string> _seventhMerchantStockAmounts;
    private static ConfigEntry<int> _seventhMerchantRestockTime;
    public static int RestockTime => _restockTime.Value;
    public static string FirstMerchantOutputItems => _firstMerchantOutputItems.Value;
    public static string FirstMerchantOutputAmounts => _firstMerchantOutputAmounts.Value;
    public static string FirstMerchantInputItems => _firstMerchantInputItems.Value;
    public static string FirstMerchantInputAmounts => _firstMerchantInputAmounts.Value;
    public static string FirstMerchantStockAmounts => _firstMerchantStockAmounts.Value;
    public static int FirstMerchantRestockTime => _firstMerchantRestockTime.Value;

    public static string SecondMerchantOutputItems => _secondMerchantOutputItems.Value;
    public static string SecondMerchantOutputAmounts => _secondMerchantOutputAmounts.Value;
    public static string SecondMerchantInputItems => _secondMerchantInputItems.Value;
    public static string SecondMerchantInputAmounts => _secondMerchantInputAmounts.Value;
    public static string SecondMerchantStockAmounts => _secondMerchantStockAmounts.Value;
    public static int SecondMerchantRestockTime => _secondMerchantRestockTime.Value;

    public static string ThirdMerchantOutputItems => _thirdMerchantOutputItems.Value;
    public static string ThirdMerchantOutputAmounts => _thirdMerchantOutputAmounts.Value;
    public static string ThirdMerchantInputItems => _thirdMerchantInputItems.Value;
    public static string ThirdMerchantInputAmounts => _thirdMerchantInputAmounts.Value;
    public static string ThirdMerchantStockAmounts => _thirdMerchantStockAmounts.Value;
    public static int ThirdMerchantRestockTime => _thirdMerchantRestockTime.Value;

    public static string FourthMerchantOutputItems => _fourthMerchantOutputItems.Value;
    public static string FourthMerchantOutputAmounts => _fourthMerchantOutputAmounts.Value;
    public static string FourthMerchantInputItems => _fourthMerchantInputItems.Value;
    public static string FourthMerchantInputAmounts => _fourthMerchantInputAmounts.Value;
    public static string FourthMerchantStockAmounts => _fourthMerchantStockAmounts.Value;
    public static int FourthMerchantRestockTime => _fourthMerchantRestockTime.Value;

    public static string FifthMerchantOutputItems => _fifthMerchantOutputItems.Value;
    public static string FifthMerchantOutputAmounts => _fifthMerchantOutputAmounts.Value;
    public static string FifthMerchantInputItems => _fifthMerchantInputItems.Value;
    public static string FifthMerchantInputAmounts => _fifthMerchantInputAmounts.Value;
    public static string FifthMerchantStockAmounts => _fifthMerchantStockAmounts.Value;
    public static int FifthMerchantRestockTime => _fifthMerchantRestockTime.Value;

    public static string SixthMerchantOutputItems => _sixthMerchantOutputItems.Value;

    public static string SixthMerchantOutputAmounts => _sixthMerchantOutputAmounts.Value;

    public static string SixthMerchantInputItems => _sixthMerchantInputItems.Value;

    public static string SixthMerchantInputAmounts => _sixthMerchantInputAmounts.Value;

    public static string SixthMerchantStockAmounts => _sixthMerchantStockAmounts.Value;

    public static int SixthMerchantRestockTime => _sixthMerchantRestockTime.Value;


    public static string SeventhMerchantOutputItems => _seventhMerchantOutputItems.Value;

    public static string SeventhMerchantOutputAmounts => _seventhMerchantOutputAmounts.Value;

    public static string SeventhMerchantInputItems => _seventhMerchantInputItems.Value;

    public static string SeventhMerchantInputAmounts => _seventhMerchantInputAmounts.Value;

    public static string SeventhMerchantStockAmounts => _seventhMerchantStockAmounts.Value;

    public static int SeventhMerchantRestockTime => _seventhMerchantRestockTime.Value;
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
        _restockTime = InitConfigEntry("General", "RestockTime", 240, "The restock time in minutes for merchants. Will be used if merchant specific values are left at 0.");
        _firstMerchantOutputItems = InitConfigEntry("FirstMerchant", "OutputItems", "1247086852,-1619308732,2019195024,-222860772,950358400,220001518,124616797,1954207008,-1930402723,1801132968,1630030026,-915028618,1102277512,1272855317,781586362,2099198078", "The item prefabs for the first merchant's output.");
        _firstMerchantOutputAmounts = InitConfigEntry("FirstMerchant", "OutputAmounts", "1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1", "The item amounts for the first merchant's output.");
        _firstMerchantInputItems = InitConfigEntry("FirstMerchant", "InputItems", "-182923609,-1629804427,1334469825,1488205677,-77477508,-77477508,-77477508,-77477508,-77477508,-77477508,-77477508,-77477508,-77477508,-77477508,-77477508,-77477508", "The item prefabs for the first merchant's input.");
        _firstMerchantInputAmounts = InitConfigEntry("FirstMerchant", "InputAmounts", "3,3,3,3,1,1,1,1,1,1,1,1,1,1,1,1", "The item amounts for the first merchant's input.");
        _firstMerchantStockAmounts = InitConfigEntry("FirstMerchant", "StockAmounts", "1,1,1,1,5,5,5,5,5,5,5,5,5,5,5,5", "The stock amounts for the first merchant's outputs.");

        _secondMerchantOutputItems = InitConfigEntry("SecondMerchant", "OutputItems", "28358550,28358550,28358550,28358550,28358550,28358550,28358550,28358550,28358550", "The item prefabs for the second merchant's output.");
        _secondMerchantOutputAmounts = InitConfigEntry("SecondMerchant", "OutputAmounts", "250,250,250,250,250,125,125,100,100", "The item amounts for the second merchant's output.");
        _secondMerchantInputItems = InitConfigEntry("SecondMerchant", "InputItems", "-21943750,666638454,-1260254082,-1581189572,551949280,-1461326411,1655869633,1262845777,2085163661", "The item prefabs for the second merchant's input.");
        _secondMerchantInputAmounts = InitConfigEntry("SecondMerchant", "InputAmounts", "1,1,1,1,5,5,5,500,500", "The item amounts for the second merchant's input.");
        _secondMerchantStockAmounts = InitConfigEntry("SecondMerchant", "StockAmounts", "99,99,99,99,99,99,99,99,99", "The stock amounts for the second merchant's outputs.");

        _thirdMerchantOutputItems = InitConfigEntry("ThirdMerchant", "OutputItems", "-2128818978,-1988816037,-1607893829,238268650,409678749,607559019,-2073081569,1780339680,-262204844,-548847761,-1797796642,1587354182,-1785271534,1863126275,584164197,379281083,136740861,-1814109557,821609569,-1755568324", "The item prefabs for the third merchant's output.");
        _thirdMerchantOutputAmounts = InitConfigEntry("ThirdMerchant", "OutputAmounts", "1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1", "The item amounts for the third merchant's output.");
        _thirdMerchantInputItems = InitConfigEntry("ThirdMerchant", "InputItems", "-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203,-257494203", "The item prefabs for the third merchant's input.");
        _thirdMerchantInputAmounts = InitConfigEntry("ThirdMerchant", "InputAmounts", "300,300,300,300,300,300,300,300,300,300,300,300,300,500,500,500,500,500,500,500", "The item amounts for the third merchant's input.");
        _thirdMerchantStockAmounts = InitConfigEntry("ThirdMerchant", "StockAmounts", "99,99,99,99,99,99,99,99,99,99,99,99,99,99,99,99,99,99,99,99", "The stock amounts for the third merchant's outputs.");

        _fourthMerchantOutputItems = InitConfigEntry("FourthMerchant", "OutputItems", "-257494203,-257494203,-257494203,-257494203", "The item prefabs for the fourth merchant's output.");
        _fourthMerchantOutputAmounts = InitConfigEntry("FourthMerchant", "OutputAmounts", "50,75,100,150", "The item amounts for the fourth merchant's output.");
        _fourthMerchantInputItems = InitConfigEntry("FourthMerchant", "InputItems", "-456161884,988417522,-1787563914,805157024", "The item prefabs for the fourth merchant's input.");
        _fourthMerchantInputAmounts = InitConfigEntry("FourthMerchant", "InputAmounts", "250,250,250,250", "The item amounts for the fourth merchant's input.");
        _fourthMerchantStockAmounts = InitConfigEntry("FourthMerchant", "StockAmounts", "99,99,99,99", "The stock amounts for the fourth merchant's outputs.");

        _fifthMerchantOutputItems = InitConfigEntry("FifthMerchant", "OutputItems", "-1370210913,1915695899,862477668,429052660,28358550", "The item prefabs for the fifth merchant's output.");
        _fifthMerchantOutputAmounts = InitConfigEntry("FifthMerchant", "OutputAmounts", "1,1,1500,15,250", "The item amounts for the fifth merchant's output.");
        _fifthMerchantInputItems = InitConfigEntry("FifthMerchant", "InputItems", "-257494203,-257494203,-257494203,-257494203,-257494203", "The item prefabs for the fifth merchant's input.");
        _fifthMerchantInputAmounts = InitConfigEntry("FifthMerchant", "InputAmounts", "250,250,250,250,250", "The item amounts for the fifth merchant's input.");
        _fifthMerchantStockAmounts = InitConfigEntry("FifthMerchant", "StockAmounts", "99,99,99,99,99", "The stock amounts for the fifth merchant's outputs.");

        _sixthMerchantOutputItems = InitConfigEntry("SixthMerchant", "OutputItems", "1412786604,2023809276,97169184,-147757377,-1796954295,271061481,1322254792,1957540013,1307774440", "The item prefabs for the sixth merchant's output.");
        _sixthMerchantOutputAmounts = InitConfigEntry("SixthMerchant", "OutputAmounts", "1,1,1,1,1,1,1,1,1", "The item amounts for the sixth merchant's output.");
        _sixthMerchantInputItems = InitConfigEntry("SixthMerchant", "InputItems", "1354115931,-1983566585,750542699,-2020212226,-106283194,188653143,-77477508,-77477508,-77477508", "The item prefabs for the sixth merchant's input.");
        _sixthMerchantInputAmounts = InitConfigEntry("SixthMerchant", "InputAmounts", "1,1,1,1,1,1,3,3,3", "The item amounts for the sixth merchant's input.");
        _sixthMerchantStockAmounts = InitConfigEntry("SixthMerchant", "StockAmounts", "99,99,99,99,99,99,5,5,5", "The stock amounts for the sixth merchant's outputs.");

        _seventhMerchantOutputItems = InitConfigEntry("SeventhMerchant", "OutputItems", "1354115931,-1983566585,750542699,-2020212226,-106283194,188653143,-77477508,-1629804427,1334469825,1488205677,-182923609", "The item prefabs for the seventh merchant's output.");
        _seventhMerchantOutputAmounts = InitConfigEntry("SeventhMerchant", "OutputAmounts", "1,1,1,1,1,1,1,1,1,1,1", "The item amounts for the seventh merchant's output.");
        _seventhMerchantInputItems = InitConfigEntry("SeventhMerchant", "InputItems", "28358550,28358550,28358550,28358550,28358550,28358550,28358550,28358550,28358550,28358550,28358550", "The item prefabs for the seventh merchant's input.");
        _seventhMerchantInputAmounts = InitConfigEntry("SeventhMerchant", "InputAmounts", "500,500,500,500,500,500,1000,1500,1500,1500,1500", "The item amounts for the seventh merchant's input.");
        _seventhMerchantStockAmounts = InitConfigEntry("SeventhMerchant", "StockAmounts", "99,99,99,99,99,99,99,99,99,99,99", "The stock amounts for the seventh merchant's outputs.");
    }

    // shadowmatter weapons with good animated abilities to use
    // Item_Weapon_GreatSword_T09_ShadowMatter PrefabGuid(1322254792) -328302080
    // Item_Weapon_Crossbow_T09_ShadowMatter PrefabGuid(1957540013) -1770479364
    // Item_Weapon_Spear_T09_ShadowMatter PrefabGuid(1307774440) 992015964

    // no good matches found for these weapons
    // Item_Weapon_Sword_T09_ShadowMatter PrefabGuid(-1215982687)
    // Item_Weapon_Mace_T09_ShadowMatter PrefabGuid(160471982)
    // Item_Weapon_Longbow_T09_ShadowMatter PrefabGuid(1283345494)
    // Item_Weapon_Axe_T09_ShadowMatter PrefabGuid(2100090213)
    // Item_Weapon_Reaper_T09_ShadowMatter PrefabGuid(-465491217)
    // Item_Weapon_Whip_T09_ShadowMatter PrefabGuid(567413754)
    // Item_Weapon_Pistols_T09_ShadowMatter PrefabGuid(-1265586439)
    // Item_Weapon_Slashers_T09_ShadowMatter PrefabGuid(506082542)


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