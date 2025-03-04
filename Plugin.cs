using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using VampireCommandFramework;

namespace Penumbra;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;

    // static readonly string _configFile = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME); // Merchants folder

    // config entries
    static ConfigEntry<string> _merchantRestockTimes;

    static ConfigEntry<string> _firstMerchantOutputItems;   // item prefabs for output
    static ConfigEntry<string> _firstMerchantOutputAmounts; // item amounts for output
    static ConfigEntry<string> _firstMerchantInputItems;    // item prefabs for input
    static ConfigEntry<string> _firstMerchantInputAmounts;  // item amounts for input
    static ConfigEntry<string> _firstMerchantStockAmounts;  // stock amounts for outputs

    static ConfigEntry<string> _secondMerchantOutputItems;
    static ConfigEntry<string> _secondMerchantOutputAmounts;
    static ConfigEntry<string> _secondMerchantInputItems;
    static ConfigEntry<string> _secondMerchantInputAmounts;
    static ConfigEntry<string> _secondMerchantStockAmounts;

    static ConfigEntry<string> _thirdMerchantOutputItems;
    static ConfigEntry<string> _thirdMerchantOutputAmounts;
    static ConfigEntry<string> _thirdMerchantInputItems;
    static ConfigEntry<string> _thirdMerchantInputAmounts;
    static ConfigEntry<string> _thirdMerchantStockAmounts;

    static ConfigEntry<string> _fourthMerchantOutputItems;
    static ConfigEntry<string> _fourthMerchantOutputAmounts;
    static ConfigEntry<string> _fourthMerchantInputItems;
    static ConfigEntry<string> _fourthMerchantInputAmounts;
    static ConfigEntry<string> _fourthMerchantStockAmounts;

    static ConfigEntry<string> _fifthMerchantOutputItems;
    static ConfigEntry<string> _fifthMerchantOutputAmounts;
    static ConfigEntry<string> _fifthMerchantInputItems;
    static ConfigEntry<string> _fifthMerchantInputAmounts;
    static ConfigEntry<string> _fifthMerchantStockAmounts;
    
    // getters for config backers
    public static string MerchantRestockTimes => _merchantRestockTimes.Value;
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
        // CreateDirectory(_configFile);

        _merchantRestockTimes = InitConfigEntry(
            "General",
            "RestockTime",
            "15,15,15,15,15",
            "The restock time in minutes for merchants. 0 is no restocking maybe? Idk, guess and check, this isn't released to the public for a reason :P"
        );

        // ------------------- First Merchant -------------------
        _firstMerchantOutputItems = InitConfigEntry(
            "FirstMerchant",
            "OutputItems",
            "1247086852,2019195024,-1619308732,-222860772,-651878258,-651878258,-651878258,-651878258,-1583485601,-223452038,-77477508,1270271716",
            "The item prefabs for the first merchant's output."
        );

        _firstMerchantOutputAmounts = InitConfigEntry(
            "FirstMerchant",
            "OutputAmounts",
            "1,1,1,1,3,3,3,3,900,10,1,100",
            "The item amounts for the first merchant's output."
        );

        _firstMerchantInputItems = InitConfigEntry(
            "FirstMerchant",
            "InputItems",
            "666638454,-21943750,-1581189572,-1260254082,-1260254082,-1581189572,-21943750,666638454,-257494203,660533034,-223452038,-257494203",
            "The item prefabs for the first merchant's input."
        );

        _firstMerchantInputAmounts = InitConfigEntry(
            "FirstMerchant",
            "InputAmounts",
            "7,7,7,7,5,5,5,5,300,25,300,900",
            "The item amounts for the first merchant's input."
        );

        _firstMerchantStockAmounts = InitConfigEntry(
            "FirstMerchant",
            "StockAmounts",
            "99,99,99,99,99,99,99,99,99,99,99,99",
            "The stock amounts for the first merchant's outputs."
        );

        // ------------------- Second Merchant -------------------
        _secondMerchantOutputItems = InitConfigEntry(
            "SecondMerchant",
            "OutputItems",
            "-570287766,447901086,-149778795,736318803,-1779269313,177845365,67930804,-1642545082,1332980397,-1805325497,1686577386,1501868129",
            "The item prefabs for the second merchant's output."
        );

        _secondMerchantOutputAmounts = InitConfigEntry(
            "SecondMerchant",
            "OutputAmounts",
            "10,10,10,10,10,10,10,10,900,900,350,900",
            "The item amounts for the second merchant's output."
        );

        _secondMerchantInputItems = InitConfigEntry(
            "SecondMerchant",
            "InputItems",
            "28625845,28625845,28625845,-949672483,-949672483,-571562864,-571562864,-571562864,-257494203,-257494203,-257494203,-257494203",
            "The item prefabs for the second merchant's input."
        );

        _secondMerchantInputAmounts = InitConfigEntry(
            "SecondMerchant",
            "InputAmounts",
            "300,300,300,200,200,410,410,210,250,150,480,200",
            "The item amounts for the second merchant's input."
        );

        _secondMerchantStockAmounts = InitConfigEntry(
            "SecondMerchant",
            "StockAmounts",
            "99,99,99,99,99,99,99,99,99,99,99,99",
            "The stock amounts for the second merchant's outputs."
        );

        // ------------------- Third Merchant -------------------
        _thirdMerchantOutputItems = InitConfigEntry(
            "ThirdMerchant",
            "OutputItems",
            "442700150,2061238391,-1638796801,285875674,-1810734832,1040125618,886814985,1271087499,1717016192,1048769481,1490846791,-1527315816",
            "The item prefabs for the third merchant's output."
        );

        _thirdMerchantOutputAmounts = InitConfigEntry(
            "ThirdMerchant",
            "OutputAmounts",
            "1,1,1,1,1,1,1,1,1,1,1,650",
            "The item amounts for the third merchant's output."
        );

        _thirdMerchantInputItems = InitConfigEntry(
            "ThirdMerchant",
            "InputItems",
            "-2147445292,572026243,747911021,649637190,1963988265,-1038642372,-413259500,3759455,2142983740,-1421775051,-1222824286,-257494203",
            "The item prefabs for the third merchant's input."
        );

        _thirdMerchantInputAmounts = InitConfigEntry(
            "ThirdMerchant",
            "InputAmounts",
            "5,5,5,5,5,5,5,5,5,5,5,550",
            "The item amounts for the third merchant's input."
        );

        _thirdMerchantStockAmounts = InitConfigEntry(
            "ThirdMerchant",
            "StockAmounts",
            "99,99,99,99,99,99,99,99,99,99,99,99",
            "The stock amounts for the third merchant's outputs."
        );

        // ------------------- Fourth Merchant -------------------
        _fourthMerchantOutputItems = InitConfigEntry(
            "FourthMerchant",
            "OutputItems",
            "442700150,2061238391,-1638796801,285875674,-1810734832,1040125618,886814985,1271087499,1717016192,1048769481,1490846791,-1748835106",
            "The item prefabs for the fourth merchant's output."
        );

        _fourthMerchantOutputAmounts = InitConfigEntry(
            "FourthMerchant",
            "OutputAmounts",
            "1,1,1,1,1,1,1,1,1,1,1,650",
            "The item amounts for the fourth merchant's output."
        );

        _fourthMerchantInputItems = InitConfigEntry(
            "FourthMerchant",
            "InputItems",
            "442700150,2061238391,-1638796801,285875674,-1810734832,1040125618,886814985,1271087499,1717016192,1048769481,1490846791,-257494203",
            "The item prefabs for the fourth merchant's input."
        );

        _fourthMerchantInputAmounts = InitConfigEntry(
            "FourthMerchant",
            "InputAmounts",
            "2,2,2,2,2,2,2,2,2,2,2,1440",
            "The item amounts for the fourth merchant's input."
        );

        _fourthMerchantStockAmounts = InitConfigEntry(
            "FourthMerchant",
            "StockAmounts",
            "99,99,99,99,99,99,99,99,99,99,99,99",
            "The stock amounts for the fourth merchant's outputs."
        );

        // ------------------- Fifth Merchant -------------------
        _fifthMerchantOutputItems = InitConfigEntry(
            "FifthMerchant",
            "OutputItems",
            "2099198078,781586362,1272855317,1102277512,-915028618,1630030026,1801132968,-1930402723,1954207008,124616797,220001518,950358400",
            "The item prefabs for the fifth merchant's output."
        );

        _fifthMerchantOutputAmounts = InitConfigEntry(
            "FifthMerchant",
            "OutputAmounts",
            "1,1,1,1,1,1,1,1,1,1,1,1",
            "The item amounts for the fifth merchant's output."
        );

        _fifthMerchantInputItems = InitConfigEntry(
            "FifthMerchant",
            "InputItems",
            "442700150,2061238391,-1638796801,285875674,-1810734832,1040125618,886814985,1271087499,1271087499,1717016192,1048769481,1490846791",
            "The item prefabs for the fifth merchant's input."
        );

        _fifthMerchantInputAmounts = InitConfigEntry(
            "FifthMerchant",
            "InputAmounts",
            "5,5,5,5,5,5,5,5,5,5,5,5",
            "The item amounts for the fifth merchant's input."
        );

        _fifthMerchantStockAmounts = InitConfigEntry(
            "FifthMerchant",
            "StockAmounts",
            "99,99,99,99,99,99,99,99,99,99,99,99",
            "The stock amounts for the fifth merchant's outputs."
        );
    }

    /*
    static void InitConfig()
    {
        _merchantRestockTimes = InitConfigEntry("General", "RestockTime", "0,0,0,0,0", "The restock time in minutes for merchants. 0 is no restocking maybe? Idk, guess and check, this isn't released to the public for a reason :P");
        
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
    }
    */
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
    static void CreateDirectory(string path)
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