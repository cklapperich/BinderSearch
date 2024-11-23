using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using HarmonyLib;

namespace PackControl;

public class CardOddsConfig 
{
    public Dictionary<ECardBorderType, float> BorderTypeOdds { get; set; }
    public Dictionary<ERarity, float> RarityWeights { get; set; }
    public float FoilChance { get; set; }
    public bool UseCustomRarityWeights { get; set; }
    
    public CardOddsConfig()
    {
        BorderTypeOdds = new Dictionary<ECardBorderType, float>();
        RarityWeights = new Dictionary<ERarity, float>();
        FoilChance = 0.05f;
        UseCustomRarityWeights = false;
    }

    // Deep copy constructor
    public CardOddsConfig(CardOddsConfig source)
    {
        BorderTypeOdds = new Dictionary<ECardBorderType, float>(source.BorderTypeOdds);
        RarityWeights = new Dictionary<ERarity, float>(source.RarityWeights);
        FoilChance = source.FoilChance;
        UseCustomRarityWeights = source.UseCustomRarityWeights;
    }
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    
    private ConfigFile DefaultConfig { get; set; }
    private ConfigFile OddsConfig { get; set; }
    
    public static Dictionary<string, CardOddsConfig> FirstSixOdds { get; private set; }
    public static Dictionary<string, CardOddsConfig> FinalCardOdds { get; private set; }
    
    // Global default configurations
    private static CardOddsConfig GlobalFirstSixConfig { get; set; }
    private static CardOddsConfig GlobalFinalConfig { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        DefaultConfig = Config;
        OddsConfig = new ConfigFile(
            Path.Combine(Path.GetDirectoryName(Info.Location), "cardodds.cfg"), 
            true
        );

        FirstSixOdds = new Dictionary<string, CardOddsConfig>();
        FinalCardOdds = new Dictionary<string, CardOddsConfig>();

        try 
        {
            LoadDefaultConfig();
            LoadGlobalConfig();
            LoadPackSpecificConfigs();
            LogLoadedConfig();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error loading configuration: {ex.Message}");
        }
        harmony.PatchAll();
    }

    private void LoadDefaultConfig()
    {
        // Reserved for future configuration menu implementation
    }

    private void LoadGlobalConfig()
    {
        // Load global defaults first
        GlobalFirstSixConfig = new CardOddsConfig();
        LoadConfigSection(OddsConfig, "Global.FirstSix", GlobalFirstSixConfig);

        GlobalFinalConfig = new CardOddsConfig();
        LoadConfigSection(OddsConfig, "Global.Final", GlobalFinalConfig);
    }

    private void LoadPackSpecificConfigs()
    {
        foreach (ECollectionPackType packType in Enum.GetValues(typeof(ECollectionPackType)))
        {
            if (packType == ECollectionPackType.None) continue;
            string packSection = packType.ToString();

            // First copy global configs
            FirstSixOdds[packSection] = new CardOddsConfig(GlobalFirstSixConfig);
            FinalCardOdds[packSection] = new CardOddsConfig(GlobalFinalConfig);

            // Then try to load pack-specific overrides if they exist
            if (HasPackSpecificConfig(OddsConfig, $"{packSection}.FirstSix"))
            {
                LoadConfigSection(OddsConfig, $"{packSection}.FirstSix", FirstSixOdds[packSection]);
            }

            if (HasPackSpecificConfig(OddsConfig, $"{packSection}.Final"))
            {
                LoadConfigSection(OddsConfig, $"{packSection}.Final", FinalCardOdds[packSection]);
            }
        }
    }

    private bool HasPackSpecificConfig(ConfigFile config, string section)
    {
        // Just check if the config file has this section by looking for any setting in it
        return File.ReadLines(config.ConfigFilePath)
            .Any(line => line.Trim().StartsWith($"[{section}]"));
    }

    private void LoadConfigSection(ConfigFile config, string section, CardOddsConfig odds)
    {
        // Border type odds
        odds.BorderTypeOdds[ECardBorderType.Base] = config.Bind(section, "Base", 1.0f, "").Value;
        odds.BorderTypeOdds[ECardBorderType.FirstEdition] = config.Bind(section, "FirstEdition", 0.20f, "").Value;
        odds.BorderTypeOdds[ECardBorderType.Silver] = config.Bind(section, "Silver", 0.08f, "").Value;
        odds.BorderTypeOdds[ECardBorderType.Gold] = config.Bind(section, "Gold", 0.04f, "").Value;
        odds.BorderTypeOdds[ECardBorderType.EX] = config.Bind(section, "EX", 0.01f, "").Value;
        odds.BorderTypeOdds[ECardBorderType.FullArt] = config.Bind(section, "FullArt", 0.0025f, "").Value;

        odds.UseCustomRarityWeights = config.Bind(section, "UseCustomRarityWeights", false, "").Value;

        odds.RarityWeights[ERarity.Common] = config.Bind(section, "CommonWeight", 0.25f, "").Value;
        odds.RarityWeights[ERarity.Rare] = config.Bind(section, "RareWeight", 0.25f, "").Value;
        odds.RarityWeights[ERarity.Epic] = config.Bind(section, "EpicWeight", 0.25f, "").Value;
        odds.RarityWeights[ERarity.Legendary] = config.Bind(section, "LegendaryWeight", 0.25f, "").Value;

        odds.FoilChance = config.Bind(section, "FoilChance", 0.05f, "").Value;
    }

    private void LogLoadedConfig()
    {
        // First log global settings
        Logger.LogInfo("Global Settings:");
        Logger.LogInfo("  FirstSix Cards (Default):");
        LogCardOddsConfig(GlobalFirstSixConfig, "    ");
        Logger.LogInfo("  Final Card (Default):");
        LogCardOddsConfig(GlobalFinalConfig, "    ");

        // Then log pack-specific overrides
        foreach (ECollectionPackType packType in Enum.GetValues(typeof(ECollectionPackType)))
        {
            if (packType == ECollectionPackType.None) continue;
            
            string packName = packType.ToString();
            if (HasPackSpecificConfig(OddsConfig, $"{packName}.FirstSix") || 
                HasPackSpecificConfig(OddsConfig, $"{packName}.Final"))
            {
                Logger.LogInfo($"Pack Type: {packName} (Overrides)");

                if (HasPackSpecificConfig(OddsConfig, $"{packName}.FirstSix"))
                {
                    Logger.LogInfo("  FirstSix Cards:");
                    LogCardOddsConfig(FirstSixOdds[packName], "    ");
                }

                if (HasPackSpecificConfig(OddsConfig, $"{packName}.Final"))
                {
                    Logger.LogInfo("  Final Card:");
                    LogCardOddsConfig(FinalCardOdds[packName], "    ");
                }
            }
        }
    }

    private void LogCardOddsConfig(CardOddsConfig config, string indent)
    {
        Logger.LogInfo($"{indent}Border Type Odds:");
        foreach (var borderType in config.BorderTypeOdds)
        {
            Logger.LogInfo($"{indent}{indent}{borderType.Key}: {borderType.Value:P2}");
        }

        Logger.LogInfo($"{indent}UseCustomRarityWeights: {config.UseCustomRarityWeights}");
        if (config.UseCustomRarityWeights)
        {
            Logger.LogInfo($"{indent}Rarity Weights:");
            foreach (var rarity in config.RarityWeights)
            {
                Logger.LogInfo($"{indent}{indent}{rarity.Key}: {rarity.Value:P2}");
            }
        }

        Logger.LogInfo($"{indent}Foil Chance: {config.FoilChance:P2}");
    }
}