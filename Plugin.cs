using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Stunlock.Core;
using System.Reflection;
using Unity.Mathematics;
using VampireCommandFramework;
using static Penumbra.Services.TokenService;

namespace Penumbra;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;
    public static string TokensPath { get; } = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "player_tokens.json");

    static readonly string _pluginPath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
    public class TokensConfig
    {
        public bool TokenSystem { get; }
        public bool TokenEconomy { get; }
        public PrefabGUID TokenItem { get; }
        public int TokenRatio { get; }
        public int TokenRate { get; }
        public bool DailyLogin { get; }
        public PrefabGUID DailyItem { get; }
        public int DailyQuantity { get; }
        public int UpdateInterval { get; }
        public TokensConfig()
        {
            TokenSystem = InitConfigEntry("Tokens", "EnableTokens", false, "Enable or disable token system.").Value;
            TokenEconomy = InitConfigEntry("Tokens", "TokenEconomy", false, "Enable to lock coin recipes in stations.").Value;
            TokenItem = new(InitConfigEntry("Tokens", "TokenItemReward", 576389135, "Item prefab for currency reward.").Value);
            TokenRatio = InitConfigEntry("Tokens", "TokenItemRatio", 6, "Currency/reward factor.").Value;
            TokenRate = InitConfigEntry("Tokens", "TokensPerMinute", 5, "Tokens gained per minute online.").Value;
            DailyLogin = InitConfigEntry("Tokens", "DailyLogin", false, "Enable or disable daily login incentive.").Value;
            DailyItem = new(InitConfigEntry("Tokens", "DailyItemReward", -257494203, "Item prefab for daily reward.").Value);
            DailyQuantity = InitConfigEntry("Tokens", "DailyItemQuantity", 50, "Amount rewarded for daily login.").Value;
            UpdateInterval = InitConfigEntry("Tokens", "TokenUpdateInterval", 30, "Minutes between updates.").Value;
        }
    }

    public static TokensConfig _tokensConfig;
    public class MerchantConfig
    {
        const int MAX = 25;

        string[] _outputItems = [];
        public string[] OutputItems
        {
            get => _outputItems;
            set => _outputItems = Enforce(value);
        }

        int[] _outputAmounts = [];
        public int[] OutputAmounts
        {
            get => _outputAmounts;
            set => _outputAmounts = Enforce(value);
        }

        string[] _inputItems = [];
        public string[] InputItems
        {
            get => _inputItems;
            set => _inputItems = Enforce(value);
        }

        int[] _inputAmounts = [];
        public int[] InputAmounts
        {
            get => _inputAmounts;
            set => _inputAmounts = Enforce(value);
        }

        int[] _stockAmounts = [];
        public int[] StockAmounts
        {
            get => _stockAmounts;
            set => _stockAmounts = Enforce(value);
        }

        public string Name { get; set; } = "";
        public int RestockTime { get; set; }
        public int TraderPrefab { get; set; }
        public string Position { get; set; } = "";
        public bool Roam { get; set; }

        static T[] Enforce<T>(T[] array)
        {
            array ??= [];

            if (array.Length <= MAX)
                return array;

            return array.AsSpan(0, MAX).ToArray();
        }
    }
    public static List<MerchantConfig> Merchants { get; } = [];
    public override void Load()
    {
        Instance = this;

        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        CommandRegistry.RegisterAll();

        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded!");
    }
    static void CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
    void InitConfig()
    {
        CreateDirectory(_pluginPath);
        _tokensConfig = new TokensConfig();

        LoadTokens();
        LoadMerchants();

        if (Merchants.Count == 0)
        {
            CreateDefaultMerchants();
        }

        SaveMerchants();
    }
    public static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        var entry = Instance.Config.Bind(section, key, defaultValue, description);
        var configFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(configFile))
        {
            var config = new ConfigFile(configFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                entry.Value = existingEntry.Value;
            }
        }

        return entry;
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
                Name = Config.Bind(section, "Name", "", "Name of merchant/wares").Value,
                OutputItems = outputItems.Value.Split(','),
                OutputAmounts = ParseIntArray(Config.Bind(section, "OutputAmounts", "", "Amounts for each output item.").Value),
                InputItems = Config.Bind(section, "InputItems", "", "Comma-separated item prefab IDs for input.").Value.Split(','),
                InputAmounts = ParseIntArray(Config.Bind(section, "InputAmounts", "", "Amounts for each input item.").Value),
                StockAmounts = ParseIntArray(Config.Bind(section, "StockAmounts", "", "Stock amounts for each output item.").Value),
                RestockTime = Config.Bind(section, "RestockTime", 60, "Restock time in minutes").Value,
                Roam = Config.Bind(section, "Roam", false, "Pace around or stay put.").Value,
                TraderPrefab = Config.Bind(section, "TraderPrefab", 0, "Trader prefab ID").Value,
                Position = Config.Bind(section, "Position", "", "Position of merchant spawn in world.").Value
            };

            Merchants.Add(merchant);
        }

        LogInstance.LogWarning($"Loaded {Merchants.Count} merchants!");
    }
    static int[] ParseIntArray(string value)
    {
        return [..value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(v => int.TryParse(v, out var result) ? result : 0)];
    }
    static void CreateDefaultMerchants()
    {
        Merchants.Add(new MerchantConfig
        {
            Name = "MerchantOne",
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
            Roam = false,
            TraderPrefab = 0,
            Position = ""
        });

        Merchants.Add(new MerchantConfig
        {
            Name = "MerchantTwo",
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
            Roam = false,
            TraderPrefab = 0,
            Position = ""
        });

        Merchants.Add(new MerchantConfig
        {
            Name = "MerchantThree",
            OutputItems =
            [
            "-2128818978","-1988816037","-1607893829","238268650","409678749","607559019",
            "-2073081569","1780339680","-262204844","-548847761","-1797796642","1587354182",
            "-1785271534","1863126275","584164197","379281083","136740861","-1814109557",
            "821609569","-1755568324"
            ],
            OutputAmounts = [..Enumerable.Repeat(1, 20)],
            InputItems = [..Enumerable.Repeat("-257494203", 20)],
            InputAmounts =
            [
            300,300,300,300,300,300,300,300,300,300,
            300,300,300,500,500,500,500,500,500,500
            ],
            StockAmounts = [..Enumerable.Repeat(99, 20)],
            RestockTime = 60,
            Roam = false,
            TraderPrefab = 0,
            Position = ""
        });

        Merchants.Add(new MerchantConfig
        {
            Name = "MerchantFour",
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
            Roam = false,
            TraderPrefab = 0,
            Position = ""
        });

        Merchants.Add(new MerchantConfig
        {
            Name = "MerchantFive",
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
            Roam = false,
            TraderPrefab = 0,
            Position = ""
        });

        LogInstance.LogWarning("Created default merchants!");
    }
    void SaveMerchants()
    {
        var keysToRemove = Config.Keys
            .Where(k => k.Section.StartsWith("Merchant", StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            Config.Remove(new ConfigDefinition(key.Section, key.Key));
        }

        for (int i = 0; i < Merchants.Count; i++)
        {
            string section = $"Merchant{i + 1}";
            var merchant = Merchants[i];

            Config.Bind(section, "Name", merchant.Name, "Name of merchant/wares.");
            Config.Bind(section, "OutputItems", string.Join(",", merchant.OutputItems), "Comma-separated item prefab IDs for output.");
            Config.Bind(section, "OutputAmounts", string.Join(",", merchant.OutputAmounts), "Amounts for each output item.");
            Config.Bind(section, "InputItems", string.Join(",", merchant.InputItems), "Comma-separated item prefab IDs for input.");
            Config.Bind(section, "InputAmounts", string.Join(",", merchant.InputAmounts), "Amounts for each input item.");
            Config.Bind(section, "StockAmounts", string.Join(",", merchant.StockAmounts), "Stock amounts for each output item.");
            Config.Bind(section, "RestockTime", merchant.RestockTime, "Restock time in minutes.");
            Config.Bind(section, "Roam", merchant.Roam, "Pace around or stay put.");
            Config.Bind(section, "TraderPrefab", merchant.TraderPrefab, "Trader prefab for merchant.");
            Config.Bind(section, "Position", merchant.Position, "Position of merchant spawn in world.");
        }
    }
    public void UpdateMerchantDefinition(int index, int traderGuid, float3 position)
    {
        if (index < 0 || index >= Merchants.Count) return;

        MerchantConfig merchantConfig = Merchants[index];
        merchantConfig.TraderPrefab = traderGuid;
        merchantConfig.Position = $"{position.x},{position.y},{position.z}";

        SaveMerchants();
    }
    public void ClearMerchantPosition(int index)
    {
        if (index < 0 || index >= Merchants.Count) return;

        MerchantConfig merchantConfig = Merchants[index];
        merchantConfig.Position = string.Empty;

        SaveMerchants();
    }
    public override bool Unload()
    {
        Config.Clear();
        _harmony.UnpatchSelf();

        return true;
    }
}

/*
public class MerchantConfig
{
    public string Name;
    public string[] OutputItems;
    public int[] OutputAmounts;
    public string[] InputItems;
    public int[] InputAmounts;
    public int[] StockAmounts;
    public int RestockTime;
    public int TraderPrefab;
    public string Position;
    public bool Roam;
}
*/