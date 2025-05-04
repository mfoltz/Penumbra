using BepInEx;
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
    internal static Plugin Instance { get; set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;
    public class MerchantConfig
    {
        public string[] OutputItems;
        public int[] OutputAmounts;
        public string[] InputItems;
        public int[] InputAmounts;
        public int[] StockAmounts;
        public int RestockTime;
        public bool Roam;
    }

    static readonly List<MerchantConfig> _merchants = [];
    public static List<MerchantConfig> Merchants => _merchants;
    public override void Load()
    {
        Instance = this;

        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        CommandRegistry.RegisterAll();

        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded!");
    }
    void InitConfig()
    {
        LoadMerchants();

        if (_merchants.Count == 0)
        {
            CreateDefaultMerchants();
        }

        SaveMerchants();
    }
    void LoadMerchants()
    {
        for (int i = 0; ; i++)
        {
            string section = $"Merchant{i + 1}";

            var outputItems = Config.Bind(section, "OutputItems", "", "Comma-separated item prefab IDs for output");
            if (string.IsNullOrEmpty(outputItems.Value)) break;

            MerchantConfig merchant = new()
            {
                OutputItems = outputItems.Value.Split(','),
                OutputAmounts = ParseIntArray(Config.Bind(section, "OutputAmounts", "", "Amounts for each output item").Value),
                InputItems = Config.Bind(section, "InputItems", "", "Comma-separated item prefab IDs for input").Value.Split(','),
                InputAmounts = ParseIntArray(Config.Bind(section, "InputAmounts", "", "Amounts for each input item").Value),
                StockAmounts = ParseIntArray(Config.Bind(section, "StockAmounts", "", "Stock amounts for each output item").Value),
                RestockTime = Config.Bind(section, "RestockTime", 60, "Restock time in minutes").Value,
                Roam = Config.Bind(section, "Roam", false, "Pace around or stay put.").Value
            };

            _merchants.Add(merchant);
        }

        LogInstance.LogWarning($"Loaded {_merchants.Count} merchants!");
    }
    static int[] ParseIntArray(string value)
    {
        return [..value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(v => int.TryParse(v, out var result) ? result : 0)];
    }
    static void CreateDefaultMerchants()
    {
        _merchants.Add(new MerchantConfig
        {
            OutputItems =
            [
            "1247086852","-1619308732","2019195024","-222860772","950358400","220001518",
            "124616797","1954207008","-1930402723","1801132968","1630030026","-915028618",
            "1102277512","1272855317","781586362","2099198078"
            ],
            OutputAmounts =
            [
            1,1,1,1,1,1,
            1,1,1,1,1,1,
            1,1,1,1
            ],
            InputItems =
            [
            "-182923609","-1629804427","1334469825","1488205677",
            "-77477508","-77477508","-77477508","-77477508",
            "-77477508","-77477508","-77477508","-77477508",
            "-77477508","-77477508","-77477508","-77477508"
            ],
            InputAmounts =
            [
            3,3,3,3,
            1,1,1,1,
            1,1,1,1,
            1,1,1,1
            ],
            StockAmounts =
            [
            1,1,1,1,
            5,5,5,5,
            5,5,5,5,
            5,5,5,5
            ],
            RestockTime = 60,
            Roam = false
        });

        _merchants.Add(new MerchantConfig
        {
            OutputItems =
            [
            "28358550","28358550","28358550","28358550","28358550",
            "28358550","28358550","28358550","28358550"
            ],
            OutputAmounts =
            [
            250,250,250,250,250,
            125,125,100,100
            ],
            InputItems =
            [
            "-21943750","666638454","-1260254082","-1581189572",
            "551949280","-1461326411","1655869633","1262845777","2085163661"
            ],
            InputAmounts =
            [
            1,1,1,1,5,5,5,500,500
            ],
            StockAmounts =
            [
            99,99,99,99,99,99,99,99,99
            ],
            RestockTime = 60,
            Roam = false
        });

        _merchants.Add(new MerchantConfig
        {
            OutputItems =
            [
            "-2128818978","-1988816037","-1607893829","238268650","409678749","607559019",
            "-2073081569","1780339680","-262204844","-548847761","-1797796642","1587354182",
            "-1785271534","1863126275","584164197","379281083","136740861","-1814109557",
            "821609569","-1755568324"
            ],
            OutputAmounts = [.. Enumerable.Repeat(1, 20)],
            InputItems = [.. Enumerable.Repeat("-257494203", 20)],
            InputAmounts =
            [
            300,300,300,300,300,300,300,300,300,300,
            300,300,300,500,500,500,500,500,500,500
            ],
            StockAmounts = [.. Enumerable.Repeat(99, 20)],
            RestockTime = 60,
            Roam = false
        });

        _merchants.Add(new MerchantConfig
        {
            OutputItems =
            [
            "-257494203","-257494203","-257494203","-257494203"
            ],
            OutputAmounts =
            [
            50,75,100,150
            ],
            InputItems =
            [
            "-456161884","988417522","-1787563914","805157024"
            ],
            InputAmounts =
            [
            250,250,250,250
            ],
            StockAmounts =
            [
            99,99,99,99
            ],
            RestockTime = 60,
            Roam = false
        });

        _merchants.Add(new MerchantConfig
        {
            OutputItems =
            [
            "-1370210913","1915695899","862477668","429052660","28358550"
            ],
            OutputAmounts =
            [
            1,1,1500,15,250
            ],
            InputItems =
            [
            "-257494203","-257494203","-257494203","-257494203","-257494203"
            ],
            InputAmounts =
            [
            250,250,250,250,250
            ],
            StockAmounts =
            [
            99,99,99,99,99
            ],
            RestockTime = 60,
            Roam = false
        });

        LogInstance.LogWarning("Created default merchants!");
    }
    void SaveMerchants()
    {
        Config.Clear();

        for (int i = 0; i < _merchants.Count; i++)
        {
            string section = $"Merchant{i + 1}";
            var merchant = _merchants[i];

            Config.Bind(section, "OutputItems", string.Join(",", merchant.OutputItems), "Comma-separated item prefab IDs for output.");
            Config.Bind(section, "OutputAmounts", string.Join(",", merchant.OutputAmounts), "Amounts for each output item.");
            Config.Bind(section, "InputItems", string.Join(",", merchant.InputItems), "Comma-separated item prefab IDs for input.");
            Config.Bind(section, "InputAmounts", string.Join(",", merchant.InputAmounts), "Amounts for each input item.");
            Config.Bind(section, "StockAmounts", string.Join(",", merchant.StockAmounts), "Stock amounts for each output item.");
            Config.Bind(section, "RestockTime", merchant.RestockTime, "Restock time in minutes.");
            Config.Bind(section, "Roam", merchant.Roam, "Pace around or stay put.");
        }

        // LogInstance.LogWarning($"Saved {_merchants.Count} merchants!");
    }
    public override bool Unload()
    {
        Config.Clear();
        _harmony.UnpatchSelf();

        return true;
    }
}