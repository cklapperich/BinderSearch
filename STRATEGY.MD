goal is to prefix patch the function "getpackcontents" in "cardexpansionselectscreen.cs

# TODO

- custom odds for ghosts
- custom ghost card pool for each collectionpack type??
- 'box topper' feature: first card of a box is guaranteed to be a fullart
- BIG proejct: Reweight box and booster prices, or reweight card prices via statistics
    - first need to read in the user's save file and calculate the average pack price for each pack?
    - then for each pack (or each card?) reweight based on the odds of pulling it. but you can pull the same card from multiple packs! sooo...
    - shorter term: let people readjust pricse via ini or set a pack price multiplier
- allow duplicates feature - already implemented! just needs a CONFIG file update
- rewrite config to allow for potential future or custom rarities

- custom card lists - likely a future mod! not likely a feature of this mod

# Ini file
Each section is named for a value in eCollectionPackType
each section contaisn 2 subsections: each substion is named FINAL or NORMAL
ini is loaded by pluggin.
ini is stored as a dictionary 
Dictionary is accessble by the patches via: Plugin.CardOdds[CollectionpackType,isFinalpack,RarityWeights]
The value of each dict is another dict that stores
a tuple of (bordertype, [common_chance,rare_chance,epic_chance,legend_chance],None)
odds default to the base game odds

DEFAULT ODDS
foil: 5% for each card
//    - FullArt: 0.25%
//    - EX: 1%
//    - Gold: 4%
//    - Silver: 8%
//    - FirstEdition: 20%
//    - Base: Default

each pack lets you weight the 4 different 'card rarity' lists. In the base game, 100% chance of a rare card from a rare pack
but we want to let you weight that list obviously
Remember 'rarity' is essentially just a list of cards and has nothing to do with pull probabilities in the base game.

common = 0.25
rare - 0.25
epic = 0.25
legendary - 0.25

This can be specified separately for firstsix and final card
# Ghosts
ghost odds stay identical to base game for now

if a ghost is rolled it replaces the final card regardless of our custom card logic


# Accessing Card Lists

This is accessed through static methods like:
csharpCopyInventoryBase.GetShownMonsterList(cardExpansionType)
InventoryBase.GetMonsterData(monsterType)
InventoryBase.GetCardUISetting(cardExpansionType)

In a later mod, we can replace getshownmonsterlist

# DEAD CODE

openscreen is called from update and ismultipack is always false... deadcode