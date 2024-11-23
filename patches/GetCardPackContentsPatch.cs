    using BepInEx.Logging;
    using System.Text;
    using HarmonyLib;
    using UnityEngine;
    using System.Collections.Generic;
    using System;
    using Random = UnityEngine.Random;
    // Game-specific enums and types
    using static ECardExpansionType;
    using static ECollectionPackType;
    using static ECardBorderType;
    using static ERarity;
    using static EMonsterType;
    using static EItemType;

    /// <summary>
    /// Harmony prefix patch for the GetPackContent method that controls card generation.
    /// </summary>
    /// <param name="clearList">If true, clears the target card list before generating new cards</param>
    /// <param name="isPremiumPack">Unused parameter in base game, likely vestigial</param>
    /// <param name="isSecondaryRolledData">Controls ghost card generation behavior:
    /// When true:
    ///   - Generates cards into m_SecondaryRolledCardDataList instead of m_RolledCardDataList
    ///   - Should only generate ghost cards (ECardExpansionType.Ghost)
    ///   - Used by OpenScreen() when a ghost card roll succeeds (1/10000 for Destiny, 1/20000 for Tetramon)
    ///   - OpenScreen() will then randomly select one ghost card to replace the final card of the main pack
    /// When false:
    ///   - Normal pack generation into m_RolledCardDataList</param>
    /// <param name="overrideCollectionPackType">When provided, overrides the pack type for generation.
    /// Used specifically with ECollectionPackType.GhostPack during ghost card generation</param>


namespace PackControl
{
    public static class CardGenerator 
    {
        /// <summary>
        /// Generates a single card based on provided configuration odds
        /// </summary>
        public static CardData GenerateCard(
            CardOddsConfig odds,
            ECollectionPackType packType,
            ECardExpansionType expansionType,
            Dictionary<ERarity, List<EMonsterType>> monsterPools,
            CardData cardDataToPopulate,
            bool allowDuplicates = false)
        {
            try
            {
                // 1. Determine rarity (using base game logic for now)git remote add origin git@gitlab.com:cklapperichmn/PackControl.git

                ERarity rarity = DetermineCardRarity(packType, odds);

                // 2. Select monster of that rarity
                EMonsterType monsterType = SelectMonsterType(monsterPools[rarity], allowDuplicates);

                // 3. Determine border type based on config odds
                ECardBorderType borderType = DetermineBorderType(odds.BorderTypeOdds, expansionType);

                // 4. Determine foil status based on config
                bool isFoil = DetermineFoilStatus(odds.FoilChance);

                // 5. Populate the provided CardData instance
                cardDataToPopulate.monsterType = monsterType;
                cardDataToPopulate.borderType = borderType;
                cardDataToPopulate.isFoil = isFoil;
                cardDataToPopulate.expansionType = expansionType;
                cardDataToPopulate.isDestiny = DetermineIsDestiny(expansionType);

                return cardDataToPopulate;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error generating card: {ex.Message}");
                Plugin.Logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private static ERarity DetermineCardRarity(ECollectionPackType packType, CardOddsConfig odds)
        {
            // If custom rarity weights are enabled, use weighted random selection
            if (odds.UseCustomRarityWeights)
            {
                float totalWeight = 0f;
                foreach (var weight in odds.RarityWeights.Values)
                {
                    totalWeight += weight;
                }

                float roll = Random.Range(0f, totalWeight);
                float currentTotal = 0f;

                foreach (var rarityPair in odds.RarityWeights)
                {
                    currentTotal += rarityPair.Value;
                    if (roll <= currentTotal)
                    {
                        return rarityPair.Key;
                    }
                }

                // Fallback to Common if something goes wrong with weights
                return ERarity.Common;
            }

            // Otherwise use base game logic - each pack type has fixed rarity
            switch (packType)
            {
                case ECollectionPackType.BasicCardPack:
                case ECollectionPackType.DestinyBasicCardPack:
                    return ERarity.Common;

                case ECollectionPackType.RareCardPack:
                case ECollectionPackType.DestinyRareCardPack:
                    return ERarity.Rare;

                case ECollectionPackType.EpicCardPack:
                case ECollectionPackType.DestinyEpicCardPack:
                    return ERarity.Epic;

                case ECollectionPackType.LegendaryCardPack:
                case ECollectionPackType.DestinyLegendaryCardPack:
                    return ERarity.Legendary;

                default:
                    return ERarity.Common;
            }
        }
        private static EMonsterType SelectMonsterType(List<EMonsterType> availableMonsters, bool allowDuplicates)
        {
            if (availableMonsters == null || availableMonsters.Count == 0)
            {
                return EMonsterType.None;
            }

            int index = Random.Range(0, availableMonsters.Count);
            EMonsterType selectedMonster = availableMonsters[index];

            if (!allowDuplicates)
            {
                availableMonsters.RemoveAt(index);
            }

            return selectedMonster;
        }

        private static ECardBorderType DetermineBorderType(
            Dictionary<ECardBorderType, float> borderOdds,
            ECardExpansionType expansionType)
        {
            // Ghost packs always get base border
            if (expansionType == ECardExpansionType.Ghost)
            {
                return ECardBorderType.Base;
            }

            float roll = Random.Range(0f, 1f);
            float cumulativeChance = 0f;

            // Try each border type in order of rarity (most rare first)
            foreach (var borderType in new[] { 
                ECardBorderType.FullArt,
                ECardBorderType.EX,
                ECardBorderType.Gold,
                ECardBorderType.Silver,
                ECardBorderType.FirstEdition
            })
            {
                if (borderOdds.TryGetValue(borderType, out float chance))
                {
                    cumulativeChance += chance;
                    if (roll < cumulativeChance)
                    {
                        return borderType;
                    }
                }
            }

            // Default to Base if no other border type was selected
            return ECardBorderType.Base;
        }

        private static bool DetermineFoilStatus(float foilChance)
        {
            return Random.Range(0f, 1f) < foilChance;
        }

        private static bool DetermineIsDestiny(ECardExpansionType expansionType)
        {
            switch (expansionType)
            {
                case ECardExpansionType.Tetramon:
                    return false;
                case ECardExpansionType.Destiny:
                    return true;
                case ECardExpansionType.Ghost:
                    // 50/50 chance between White Ghost and Black Ghost
                    return Random.Range(0, 100) < 50;
                default:
                    return false;
            }
        }

        public static void LogCardGeneration(CardData card, int index, bool isFinalCard)
        {
            Plugin.Logger.LogWarning(
                $"Card {index}{(isFinalCard ? " (FINAL)" : "")}: " +
                $"Monster={card.monsterType}, " +
                $"Border={card.borderType}, " +
                $"Rarity={InventoryBase.GetMonsterData(card.monsterType)?.Rarity}, " +
                $"Foil={card.isFoil}, " +
                $"Destiny={card.isDestiny}, " +
                $"Expansion={card.expansionType}");
        }
    }
    [HarmonyPatch(typeof(CardOpeningSequence))]
    [HarmonyPatch("GetPackContent")]
    public class GetPackContentPatch
    {
        private static bool Prefix(
            CardOpeningSequence __instance,
            bool clearList,
            bool isPremiumPack,
            bool isSecondaryRolledData,
            ECollectionPackType overrideCollectionPackType,
            ref List<CardData> ___m_RolledCardDataList,
            ref List<CardData> ___m_SecondaryRolledCardDataList,
            ref List<CardData> ___m_CardDataPool,
            ref List<CardData> ___m_CardDataPool2,
            ref List<float> ___m_CardValueList,
            ref bool ___m_HasFoilCard,
            ref ECollectionPackType ___m_CollectionPackType)
        {
            try
            {
                // Plugin.Logger.LogWarning($"GetPackContent called with: clearList={clearList}, " +
                //     $"isPremiumPack={isPremiumPack}, isSecondaryRolledData={isSecondaryRolledData}, " +
                //     $"overridePackType={overrideCollectionPackType}, currentPackType={___m_CollectionPackType}");

                // Validate assumptions
                if (isPremiumPack)
                {
                    Plugin.Logger.LogWarning("Unexpected premium pack encountered!");
                    return true; // Let original method handle this case
                }

                // Clear lists if needed
                if (clearList)
                {
                    if (isSecondaryRolledData)
                    {
                        ___m_SecondaryRolledCardDataList.Clear();
                    }
                    else
                    {
                        ___m_RolledCardDataList.Clear();
                        ___m_CardValueList.Clear();
                    }
                }

                // Get expansion type and settings
                ECardExpansionType expansionType = isSecondaryRolledData ? 
                    InventoryBase.GetCardExpansionType(overrideCollectionPackType) :
                    InventoryBase.GetCardExpansionType(___m_CollectionPackType);

                var cardUISetting = InventoryBase.GetCardUISetting(expansionType);
                var packConfigKey = ___m_CollectionPackType.ToString();

                // Get monster pools organized by rarity
                var monstersByRarity = GetMonsterListsByRarity(expansionType);

                // Get odds for this pack type
                var firstSixOdds = Plugin.FirstSixOdds.ContainsKey(packConfigKey) ? 
                    Plugin.FirstSixOdds[packConfigKey] : 
                    Plugin.FirstSixOdds["Global.FirstSix"];

                var finalCardOdds = Plugin.FinalCardOdds.ContainsKey(packConfigKey) ? 
                    Plugin.FinalCardOdds[packConfigKey] : 
                    Plugin.FinalCardOdds["Global.Final"];

                // Generate cards
                for (int i = 0; i < 7; i++)
                {
                    bool isFinalCard = (i == 6);
                    var odds = isFinalCard ? finalCardOdds : firstSixOdds;
                    
                    // Get CardData from appropriate pool
                    CardData cardData = isSecondaryRolledData ? 
                        ___m_CardDataPool2[i] : 
                        ___m_CardDataPool[i];

                    // Generate the card
                    cardData = CardGenerator.GenerateCard(
                        odds,
                        ___m_CollectionPackType,
                        expansionType,
                        monstersByRarity,
                        cardData,
                        cardUISetting.openPackCanHaveDuplicate
                    );

                    // Update foil tracking
                    if (cardData.isFoil)
                    {
                        ___m_HasFoilCard = true;
                    }

                    // Add to appropriate list
                    if (isSecondaryRolledData)
                    {
                        ___m_SecondaryRolledCardDataList.Add(cardData);
                    }
                    else 
                    {
                        ___m_RolledCardDataList.Add(cardData);
                        ___m_CardValueList.Add(CPlayerData.GetCardMarketPrice(cardData));
                    }

                    //CardGenerator.LogCardGeneration(cardData, i, isFinalCard);
                }

                // Match original GC behavior
                if (CPlayerData.m_GameReportDataCollectPermanent.cardPackOpened % 10 == 9)
                {
                    GC.Collect();
                }

                return false; // Don't run original method
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in GetPackContent patch: {ex.Message}");
                Plugin.Logger.LogError(ex.StackTrace);
                return true; // Let original method handle errors
            }
        }

            private static Dictionary<ERarity, List<EMonsterType>> GetMonsterListsByRarity(ECardExpansionType expansionType)
            {
                var result = new Dictionary<ERarity, List<EMonsterType>>();
                var allMonsters = InventoryBase.GetShownMonsterList(expansionType);

                foreach (ERarity rarity in Enum.GetValues(typeof(ERarity)))
                {
                    if (rarity != ERarity.None && rarity != ERarity.SuperLegend)
                    {
                        result[rarity] = new List<EMonsterType>();
                    }
                }

                foreach (var monsterType in allMonsters)
                {
                    var monsterData = InventoryBase.GetMonsterData(monsterType);
                    if (monsterData != null)
                    {
                        result[monsterData.Rarity].Add(monsterType);
                    }
                }

                return result;
            }

            private static EMonsterType SelectMonsterType(List<EMonsterType> availableMonsters, bool allowDuplicates)
            {
                if (availableMonsters == null || availableMonsters.Count == 0)
                {
                    return EMonsterType.None;
                }

                int index = UnityEngine.Random.Range(0, availableMonsters.Count);
                EMonsterType selectedMonster = availableMonsters[index];

                if (!allowDuplicates)
                {
                    availableMonsters.RemoveAt(index);
                }

                return selectedMonster;
            }

            private static ECardBorderType DetermineBorderType(ECardExpansionType expansionType)
            {
                // For now, using base game probabilities
                float roll = UnityEngine.Random.Range(0f, 1f);
                
                if (expansionType == ECardExpansionType.Ghost)
                    return ECardBorderType.Base;

                if (roll < 0.0025f) return ECardBorderType.FullArt;
                if (roll < 0.0125f) return ECardBorderType.EX;
                if (roll < 0.0525f) return ECardBorderType.Gold;
                if (roll < 0.1325f) return ECardBorderType.Silver;
                if (roll < 0.3325f) return ECardBorderType.FirstEdition;
                
                return ECardBorderType.Base;
            }

            private static bool DetermineFoilStatus(bool isFinalCard, string packType)
            {
                if (isFinalCard)
                {
                    return true; // UnityEngine.Random.Range(0f, 1f) < Plugin.FinalCardOdds[packType].FoilChance;
                }
                return UnityEngine.Random.Range(0f, 1f) < 0.05f; // Base game 5% chance
            }

            private static bool DetermineIsDestiny(ECardExpansionType expansionType)
            {
                switch (expansionType)
                {
                    case ECardExpansionType.Tetramon:
                        return false;
                    case ECardExpansionType.Destiny:
                        return true;
                    // This just determines if the card is a Black Ghost or a White Ghost!! Very deceptive variable name !!
                    // which then determines the cards "Element" - GhostWhite or GhostBlack
                    case ECardExpansionType.Ghost:
                        return UnityEngine.Random.Range(0, 100) < 50;
                    default:
                        return false;
                }
            }
            private static void LogMonsterPools(Dictionary<ERarity, List<EMonsterType>> pools)
            {
                StringBuilder sb = new StringBuilder("Monster pools by rarity:\n");
                foreach (var pool in pools)
                {
                    sb.AppendLine($"{pool.Key}: {pool.Value.Count} monsters available");
                }
                Plugin.Logger.LogWarning(sb.ToString());
            }

            private static void LogCardGeneration(int index, CardData card, bool isFinalCard)
            {
                Plugin.Logger.LogWarning(
                    $"Card {index}{(isFinalCard ? " (FINAL)" : "")}: " +
                    $"Monster={card.monsterType}, " +
                    $"Border={card.borderType}, " +
                    $"Rarity={InventoryBase.GetMonsterData(card.monsterType)?.Rarity}, " +
                    $"Foil={card.isFoil}, " +
                    $"Destiny={card.isDestiny}, " +
                    $"Expansion={card.expansionType}");
            }
        }
    }