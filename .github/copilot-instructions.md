General Instructions 
- Always attempt a build when completing code changes
- When The build is successful, update the CHANGELOG.md 
- Check to make sure if the README.md needs to be updated. 

The unpacked game assets are in the game_folder\Stardew Valley\Content (unpacked) and are a good palce to look for assets as they are the same in the actual game. 

Basic techniques

Tracking changes to a value

Mods often need to know when a value changed. If there's no SMAPI event for the value, you can create a private field to track the value, and update it using the update tick event.


Items

Items are objects which represent things which can be put in an inventory. Tools, Crops, etc.

Create an item (Object)

All constructors for Object:

 public Object(Vector2 tileLocation, int parentSheetIndex, int initialStack);
 public Object(Vector2 tileLocation, int parentSheetIndex, bool isRecipe = false);
 public Object(int parentSheetIndex, int initialStack, bool isRecipe = false, int price = -1, int quality = 0);
 public Object(Vector2 tileLocation, int parentSheetIndex, string Givenname, bool canBeSetDown, bool canBeGrabbed, bool isHoedirt, bool isSpawnedObject);
Where parentSheetIndex is the ID of the item (can be found in ObjectInformation.xnb).

Spawn an item on the ground

You can spawn an item on the ground with the GameLocation class's dropObject method:

 public virtual bool dropObject(Object obj, Vector2 dropLocation, xTile.Dimensions.Rectangle viewport, bool initialPlacement, Farmer who = null);

 // Concrete code for spawning:
 Game1.getLocationFromName("Farm").dropObject(new StardewValley.Object(itemId, 1, false, -1, 0), new Vector2(x, y) * 64f, Game1.viewport, true, (Farmer)null);

Add an item to an inventory

//You can add items found in ObjectInformation using:
    Game1.player.addItemByMenuIfNecessary((Item)new StardewValley.Object(int parentSheetIndex, int initialStack, [bool isRecipe = false], [int price = -1], [int quality = 0]));

Another example:

    // Add a weapon directly into player's inventory
    const int WEAP_ID = 19;                  // Shadow Dagger -- see Data/weapons
    Item weapon = new MeleeWeapon(WEAP_ID);  // MeleeWeapon is a class in StardewValley.Tools
    Game1.player.addItemByMenuIfNeceusssea ry(weapon);

Remove an item from an inventory

This is dependent on the inventory - rarely will you be calling this directly, as the game has functions for this for the Player, located in Farmer (in the main namespace).

To do so, in most situations, just call .removeItemFromInventory(Item)

Valid fields
These are used in various places like machine data and shop data. These can only be used if the field's docs specifically mention that it allows item queries.

Query format
An item query consists of a string containing a query name with zero or more arguments. See the list of queries below.

⚠ Item queries are partly case-sensitive. While some values are case-insensitive, this isn't consistent. Using the exact capitalization is recommended to avoid issues.

Argument format
Item queries can take space-delimited arguments. For example, RANDOM_ITEMS (F) 1376 1390 has three arguments: (F), 1376, and 1390.

If you have spaces within an argument, you can surround it with quotes to keep it together. For example, LOST_BOOK_OR_ITEM "RANDOM_ITEMS (O)" passes RANDOM_ITEMS (O) as one argument. You can escape inner quotes with backslashes if needed.

Remember that quotes and backslashes inside JSON strings need to be escaped too. For example, "ItemId": "LOST_BOOK_OR_ITEM \"RANDOM_ITEMS (O)\"" will send LOST_BOOK_OR_ITEM "RANDOM_ITEMS (O)" to the game code. Alternatively, you can use single-quotes for the JSON string instead, like "ItemId": 'LOST_BOOK_OR_ITEM "RANDOM_ITEMS (O)"'.

Available queries
General use
query	effect
ALL_ITEMS [type ID] [flags]	Every item provided by the item data definitions. If [type ID] is set to an item type identifier (like (O) for object), only returns items from the matching item data definition.
The [flags] specify options to apply. If specified, they must be at the end of the argument list (with or without [type ID]). The flags can be any combination of:

flag	effect
@isRandomSale	Don't return items marked 'exclude from random sale' in Data/Furniture or Data/Objects.
@requirePrice	Don't return items with a sell-to-player price below Gold.png1g.
For example:

ALL_ITEMS will return every item in the game.
ALL_ITEMS @isRandomSale will return every item in the game that's not excluded from random sale.
ALL_ITEMS (F) @isRandomSale will return every furniture item in the game that's not excluded from random sale.
FLAVORED_ITEM <type> <ingredient ID> [ingredient flavor ID]	A flavored item like Apple Wine. The <type> can be one of Wine, Jelly, Pickle, Juice, Roe, AgedRoe, Honey, Bait, DriedFruit, DriedMushroom, or SmokedFish. The <ingredient ID> is the qualified or unqualified item ID which provides the flavor (like Apple in Apple Wine). For Honey, you can set the <flavor ID> to -1 for Wild Honey.
For aged roe only, the [ingredient flavor ID] is the flavor of the <ingredient ID>. For example, FLAVORED_ITEM AgedRoe (O)812 128 creates Aged Pufferfish Roe (812 is roe and 128 is pufferfish).

RANDOM_ITEMS <type definition ID> [min ID] [max ID] [flags]	All items from the given type definition ID in randomized order, optionally filtered to those with a numeric ID in the given [min ID] and [max ID] range (inclusive).
The [flags] specify options to apply. If specified, they must be at the end of the argument list (with or without [min ID] and/or [max ID]). The flags can be any combination of:

The flags can be any combination of:

flag	effect
@isRandomSale	Don't return items marked 'exclude from random sale' in Data/Furniture or Data/Objects.
@requirePrice	Don't return items with a sell-to-player price below Gold.png1g.
For example, you can sell a random wallpaper for Gold.png200g in Data/Shops:

{
    "ItemId": "RANDOM_ITEMS (WP)",
    "MaxItems": 1,
    "Price": 200
}
Or a random house plant:

{
    "ItemId": "RANDOM_ITEMS (F) 1376 1390",
    "MaxItems": 1
}
Or a random custom item added by a mod by its item ID prefix:

{
    "ItemId": "RANDOM_ITEMS (O)",
    "MaxItems": 1,
    "PerItemCondition": "ITEM_ID_PREFIX Target AuthorName_ModName_"
}
Or 10 random objects with any category except -13 or -14:

{
    "ItemId": "RANDOM_ITEMS (O)",
    "MaxItems": 10,
    "PerItemCondition": "ITEM_CATEGORY, !ITEM_CATEGORY Target -13 -14"
}
Specific items
query	effect
DISH_OF_THE_DAY	The Saloon's dish of the day.
LOST_BOOK_OR_ITEM [alternate query]	A lost book if the player hasn't found them all yet, else the result of the [alternate query] if specified, else nothing.
For example, LOST_BOOK_OR_ITEM (O)770 returns mixed seeds if the player found every book already.

RANDOM_BASE_SEASON_ITEM	A random seasonal vanilla item which can be found by searching garbage cans, breaking containers in the mines, etc.
SECRET_NOTE_OR_ITEM [alternate query]	A secret note (or journal scrap on the island) if the player hasn't found them all yet, else the result of the [alternate query] if specified, else nothing.
For example, SECRET_NOTE_OR_ITEM (O)390 returns clay if the player found every secret note already.

SHOP_TOWN_KEY	The special town key item. This is only valid in shops.
Specialized
query	effect
ITEMS_SOLD_BY_PLAYER <shop location>	Random items the player has recently sold to the <shop location>, which can be one of SeedShop (Pierre's store) or FishShop (Willy's fish shop).
LOCATION_FISH <location> <bobber tile> <depth>	A random item that can be found by fishing in the given location. The <location> should be the internal name of the location, <bobber tile> is the position of the fishing rod's bobber in the water (in the form <x> <y>), and <depth> is the bobber's distance from the nearest shore measured in tiles (where 0 is directly adjacent to the shore).
Careful: since the target location might use LOCATION_FISH queries in its list, it's easy to cause a circular reference by mistake (e.g. location A gets fish from B, which gets fish from A). If this happens, the game will log an error and return no item.

MONSTER_SLAYER_REWARDS	All items unlocked by monster eradication goals which have been completed and collected from Gil by the current player at the current time. The list sort order follows the order of monsters in MonsterSlayerQuests.xnb, e.g., Slime Ring, Savage Ring, Burglar Ring, etc.
MOVIE_CONCESSIONS_FOR_GUEST [NPC name]	Get the movie concessions shown when watching a movie with the given [NPC name]. If omitted, the NPC defaults to the one currently invited to watch a movie (or Abigail if none).
RANDOM_ARTIFACT_FOR_DIG_SPOT	A random item which is defined in Data/Objects with the Arch (artifact) type, and whose spawn rules in the Miscellaneous field match the current location and whose random probability passes. This is mainly used by artifact spots.
TOOL_UPGRADES [tool ID]	The tool upgrades listed in Data/Shops whose conditions match the player's inventory (i.e. the same rules as Clint's tool upgrade shop). If [tool ID] is specified, only upgrades which consume that tool ID are shown.
Item spawn fields
Item spawn fields are a common set of fields to use item queries in data assets like machines and shops. These are only available for data assets which specifically mention they support item spawn fields in their docs.

field	effect
ID	The unique string ID for this entry (not the item itself) within the current list.
This is semi-optional — if omitted, it'll be auto-generated from the ItemId, RandomItemId, and IsRecipe fields. However multiple entries with the same ID may cause unintended behavior (e.g. shop items reducing each others' stock limits), so it's often a good idea to set a globally unique ID instead.

ItemId	One of:
the qualified or unqualified item ID (like (O)128 for a pufferfish);
or an item query to dynamically choose one or more items.
RandomItemId	(Optional) A list of item IDs to randomly choose from, using the same format as ItemId (including item queries). If set, ItemId is optional and ignored. Each entry in the list has an equal probability of being chosen. For example:
// wood, stone, or pizza
"RandomItemId": [ "(O)388", "(O)390", "(O)206" ]
Condition	(Optional) A game state query which indicates whether this entry should be applied. Defaults to always true.
Note: not supported for weapon projectiles.

PerItemCondition	(Optional) A game state query which indicates whether an item produced from the other fields should be returned. Defaults to always true.
For example, this can be used to filter queries like RANDOM_ITEMS:

// random mineral
"ItemId": "RANDOM_ITEMS (O)",
"PerItemCondition": "ITEM_CATEGORY Target -12"
MaxItems	(Optional) If this entry produces multiple separate item stacks, the maximum number to return. (This does not affect the size of each stack; see MinStack and MaxStack for that.) Default unlimited.
IsRecipe	(Optional) Whether to get the crafting/cooking recipe for the item, instead of the item itself. Default false.
The game will unlock the recipe with the ID equal to the item query's ObjectInternalName field, or the target item's internal Name (which defaults to its ID) if not set.

Quality	(Optional) The quality of the item to find. One of 0 (normal), 1 (silver), 2 (gold), or 4 (iridium). Invalid values will snap to the closest valid one (e.g. 7 will become iridium). Default -1, which keeps the value set by the item query (usually 0).
MinStack	(Optional) The item's minimum and default stack size. Default -1, which keeps the value set by the item query (usually 1).
MaxStack	(Optional) If set to a value higher than MinStack, the stack is set to a random value between them (inclusively). Default -1.
ObjectInternalName	(Optional) For objects only, the internal name to use. Defaults to the item's name in Data/Objects.
ObjectDisplayName	(Optional) For objects only, a tokenizable string for the item's display name. Defaults to the item's display name in Data/Objects. This can optionally contain %DISPLAY_NAME (the item's default display name) and %PRESERVED_DISPLAY_NAME (the preserved item's display name if applicable, e.g. if set via PreserveId in machine data).
Careful: text in this field will be saved permanently in the object's info and won't be updated when the player changes language or the content pack changes. That includes Content Patcher translations (like %DISPLAY_NAME {{i18n: wine}}), which will save the translated text for the current language. Instead, add the text to a strings asset like Strings/Objects and then use the [LocalizedText] token.

For example, here's how you'd create flavored oils with Content Patcher: 
ToolUpgradeLevel	(Optional) For tools only, the initial upgrade level for the tool when created (like Copper vs Gold Axe, or Training Rod vs Iridium Rod). Default -1, which keeps the value set by the item query (usually 0).
QualityModifiers
StackModifiers	(Optional) Quantity modifiers applied to the Quality or Stack value. Default none.
The quality modifiers operate on the numeric quality values (i.e. 0 = normal, 1 = silver, 2 = gold, and 4 = iridium). For example, silver × 2 is gold.

QualityModifierMode
StackModifierMode	(Optional) Quantity modifier modes which indicate what to do if multiple modifiers in the QualityModifiers or StackModifiers field apply at the same time. Default Stack.
ModData	(Optional) The mod data fields to add to created items. Default none.
For example:

"ModData": {
    "Example.ModId_FieldName": "some custom data"
}
For C# mod authors
Use queries in custom data assets
You can use the ItemQueryResolver class to parse item queries.

For example, let's say a custom data model uses item spawn fields to choose which gifts are added to the starting gift box:

public class InitialGiftsModel
{
    public List<GenericSpawnItemData> Items = new();
}
You can spawn items from it like this:

ItemQueryContext itemQueryContext = new();
foreach (GenericSpawnItemData entry in model.Items)
{
    Item item = ItemQueryResolver.TryResolveRandomItem(entry, itemQueryContext, logError: (query, message) => this.Monitor.Log($"Failed parsing item query '{query}': {message}", LogLevel.Warn));
    // or TryResolve to get all items
}
You can also use GenericSpawnItemDataWithCondition to combine it with game state queries:

ItemQueryContext itemQueryContext = new();
foreach (GenericSpawnItemDataWithCondition entry in model.Items)
{
    if (!GameStateQuery.CheckConditions(entry.Condition))
        continue;

    Item item = ItemQueryResolver.TryResolveRandomItem(entry, itemQueryContext, logError: (query, message) => this.Monitor.Log($"Failed parsing item query '{query}': {message}", LogLevel.Warn));
}
Add custom item queries
You can define new item queries ItemQueryResolver.Register("Example.ModId_QueryName", handleQueryMethod). To avoid conflicts, custom query names should apply the unique string ID conventions.
 Item IDs
Every item is identified in the game data using a unique item ID. This has two forms:

The unqualified item ID (item.ItemId) is a unique string ID for the item, like 128 (vanilla item) or Example.ModId_Watermelon (custom item). For legacy reasons, the unqualified ID for vanilla items may not be globally unique; for example, Pufferfish (object 128) and Mushroom Box (bigcraftable 128) both have item ID 128.
The qualified item ID (item.QualifiedItemId) is a globally unique identifier which combines the item's type ID and unqualified item ID, like (O)128 for object ID 128.
With SMAPI installed, you can run the list_items console command in-game to search item IDs.

Note: mods created before Stardew Valley 1.6 may use the item.ParentSheetIndex field as an item identifier. This is not a valid identifier; multiple items of the same type may have the same sprite index for different textures.

Item types
Items are defined by item type data definitions, which handle parsing data of a certain type. For example, the game's ObjectDataDefinition class handles producing object-type items by parsing the Data/Objects asset.

Each definition has a unique ID like (O), which is used to form globally unique qualified item IDs. In C# code, this is tracked by the item.TypeDefinitionId field, which matches ItemRegistry.type_* constants for vanilla item types.

These are the item types for which custom items can added/edited:

item type	type identifier	data asset	brief summary
Objects	(O)	Data/Objects	The most common item type. Depending on their data, they can be placed in the world, picked up, eaten, sold to shops, etc.
Big craftables	(BC)	Data/BigCraftables	Items which can be placed in the world and are two tiles tall (instead of one like objects).
Boots	(B)	Data/Boots	Items which can be equipped in the player's boots slot. These change the player sprite and may provide buffs.
Furniture	(F)	Data/Furniture	Decorative items which can be placed in the world. In some cases players can sit on them or place items on them.
Hats	(H)	Data/Hats	Items which can be equipped in the player's hat slot. These change the player sprite.
Mannequins	(M)	Data/Mannequins	Decorative items which can be placed in the world, and used to store and display clothing.
Pants	(P)	Data/Pants	Items which can be equipped in the player's pants slot. These change the player sprite.
Shirts	(S)	Data/Shirts	Items which can be equipped in the player's shirt slot. These change the player sprite.
Tools	(T)	Data/Tools	Items that can be swung or used by the player to perform some effect (e.g. dig dirt, chop trees, milk or shear animals, etc).
Trinkets	(TR)	Data/Trinkets	Items that can be equipped in the player's trinket slot to enable special effects.
Wallpaper & flooring	(WP) and (FL)	Data/AdditionalWallpaperFlooring	Items which can be applied to a decoratable location (e.g. a farmhouse or shed) to visually change its floor or wall design. (These are separate from placeable items like brick floor.)
Weapons	(W)	Data/Weapons	Items which can be swung or used by the player to damage monsters.
When resolving an unqualified item ID like 128, the game will get the first item type for which it exists in this order: object, big craftable, furniture, weapon, boots, hat, mannequin, pants, shirt, tool, trinket, wallpaper, and floorpaper.

Item sprites
For each item type, the game has two files in its Content folder (which can be unpacked for editing):

a data asset for the text data for its items (names, descriptions, prices, etc);
and a spritesheet for the in-game item icons.
Each item has a ParentSheetIndex field which is its position in the item type's spritesheet, starting at 0 in the top-left and incrementing by one as you move across and then down. For example, hat #0 is the first sprite in Characters/Farmer/hats.

Define a custom item
You can define custom items for most vanilla item types using only Content Patcher or SMAPI's content API.

For example, this content pack adds a new Pufferchick item with a custom image, custom gift tastes, and a custom crop that produces it. Note that item references in other data assets like Data/Crops and Data/NPCGiftTastes use the item ID.

{
    "Format": "2.5.0",
    "Changes": [
        // add item
        {
            "Action": "EditData",
            "Target": "Data/Objects",
            "Entries": {
                "{{ModId}}_Pufferchick": {
                    "Name": "{{ModId}}_Pufferchick", // best practice to match the ID, since it's sometimes used as an alternate ID (e.g. in Data/CraftingRecipes)
                    "Displayname": "Pufferchick",
                    "Description": "An example object.",
                    "Type": "Seeds",
                    "Category": -74,
                    "Price": 1200,

                    "Texture": "Mods/{{ModId}}/Objects",
                    "SpriteIndex": 0
                }
            }
        },

        // add gift tastes
        {
            "Action": "EditData",
            "Target": "Data/NPCGiftTastes",
            "TextOperations": [
                {
                    "Operation": "Append",
                    "Target": ["Entries", "Universal_Love"],
                    "Value": "{{ModId}}_Pufferchick",
                    "Delimiter": " " // if there are already values, add a space between them and the new one
                }
            ]
        },

        // add crop (Pufferchick is both seed and produce, like coffee beans)
        {
            "Action": "EditData",
            "Target": "Data/Crops",
            "Entries": {
                "{{ModId}}_Pufferchick": {
                    "Seasons": [ "spring", "summer", "fall" ],
                    "DaysInPhase": [ 1, 1, 1, 1, 1 ],
                    "HarvestItemId": "{{ModId}}_Pufferchick",

                    "Texture": "Mods/{{ModId}}/Crops",
                    "SpriteIndex": 0
                }
            }
        },

        // add item + crop images
        {
            "Action": "Load",
            "Target": "Mods/{{ModId}}/Crops, Mods/{{ModId}}/Objects",
            "FromFile": "assets/{{TargetWithoutPath}}.png" // assets/Crops.png, assets/Objects.png
        }
    ]
}
Most item data assets work just like Data/Objects. See also specific info for custom fruit trees, custom tools, and melee weapons.

Error items
When an item is broken (e.g. due to deleting the mod which adds it), it's represented in-game as a default Error Item with a 🛇 sprite. This keeps the previous item data in case the item data is re-added.

Common data
Quality
Each item has a quality level which (depending on the item type) may affect its price, health boost, etc. The valid qualities are:

quality	value	constant
normal	0	Object.lowQuality
silver	1	Object.medQuality
gold	2	Object.highQuality
iridium	4	Object.bestQuality
Categories
Each item also has a category (represented by a negative integer). In code, you can get an item's category value from item.Category, and its translated name from item.getCategoryName(). Here are the valid categories:

value	internal constant	context tag	English translation	Color	Properties
-2	Object.GemCategory	category_gem	Mineral	#6E005A	Affected by Gemologist profession
-4	Object.FishCategory	category_fish	Fish	#00008B (DarkBlue)	Affected by Fisher and Angler professions
-5	Object.EggCategory	category_egg	Animal Product	#FF0064	Affected by Rancher profession, can be used in a slingshot
-6	Object.MilkCategory	category_milk	Animal Product	#FF0064	Affected by Rancher profession
-7	Object.CookingCategory	category_cooking	Cooking	#DC3C00	
-8	Object.CraftingCategory	category_crafting	Crafting	#943D28	Is Placeable
-9	Object.BigCraftableCategory	category_big_craftable			Is Placeable
-12	Object.mineralsCategory	category_minerals	Mineral	#6E005A	Affected by Gemologist profession
-14	Object.meatCategory	category_meat	Animal Product	#FF0064	
-15	Object.metalResources	category_metal_resources	Resource	#406672	
-16	Object.buildingResources	category_building_resources	Resource	#406672	
-17	Object.sellAtPierres	category_sell_at_pierres			
-18	Object.sellAtPierresAndMarnies	category_sell_at_pierres_and_marnies	Animal Product	#FF0064	Affected by Rancher profession
-19	Object.fertilizerCategory	category_fertilizer	Fertilizer	#708090 (SlateGray)	Is Placeable, is always passable
-20	Object.junkCategory	category_junk	Trash	#696969 (DimGray)	
-21	Object.baitCategory	category_bait	Bait	#8B0000 (DarkRed)	Can be attached to a fishing rod
-22	Object.tackleCategory	category_tackle	Fishing Tackle	#008B8B (DarkCyan)	Can be attached to a fishing rod, cannot stack
-23	Object.sellAtFishShopCategory	category_sell_at_fish_shop			
-24	Object.furnitureCategory	category_furniture	Decor	#9650BE	
-25	Object.ingredientsCategory	category_ingredients	Cooking		
-26	Object.artisanGoodsCategory	category_artisan_goods	Artisan Goods	#009B6F	Affected by Artisan profession
-27	Object.syrupCategory	category_syrup	Artisan Goods	#009B6F	Affected by Tapper profession
-28	Object.monsterLootCategory	category_monster_loot	Monster Loot	#320A46	
-29	Object.equipmentCategory	category_equipment			
-74	Object.SeedsCategory	category_seeds	Seed	#A52A2A (Brown)	Is Placeable, is always passable
-75	Object.VegetableCategory	category_vegetable	Vegetable	#008000 (Green)	Affected by Tiller profession, can be used in a slingshot
-79	Object.FruitsCategory	category_fruits	Fruit	#FF1493 (DeepPink)	Affected by Tiller profession (if not foraged), can be used in a slingshot
-80	Object.flowersCategory	category_flowers	Flower	#DB36D3	Affected by Tiller profession
-81	Object.GreensCategory	category_greens	Forage	#0A8232	
-95	Object.hatCategory	category_hat			
-96	Object.ringCategory	category_ring			
-97	Object.bootsCategory	category_boots			
-98	Object.weaponCategory	category_weapon			
-99	Object.toolCategory	category_tool			
-100	Object.clothingCategory	category_clothing			
-101	Object.trinketCategory	category_trinket			
-102	Object.booksCategory			#552F1B	
-103	Object.skillBooksCategory			#7A5d27	
-999	Object.litterCategory	category_litter			
Console commands 
Context tags
A context tag is an arbitrary data label like category_gem or item_apple attached to items. These provide metadata about items (e.g. their color, quality, category, general groupings like alcohol or fish, etc), and may affect game logic (e.g. machine processing).

See Modding:Context tags for more info.

Specific item types
For docs about each item type (e.g. objects or weapons), see the item types table above.

For C# mods
Identify items
You can uniquely identify items by checking their item ID fields. For example:

bool isPufferfish = item.QualifiedItemId == "(O)128";
The ItemRegistry class also provides methods to work with items. For example:

// check if item would be matched by a qualified or unqualified item ID
bool isPufferfish = ItemRegistry.HasItemId(item, "128");

// qualify an item ID if needed
string pufferfishQualifiedId = ItemRegistry.QualifyItemId("128"); // returns "(O)128"
Note that flavored items like jellies and wine don't have their own ID. For example, Blueberry Wine and Wine are both (O)348. You can get the flavor item ID from the preservedParentSheetIndex field; for example, Blueberry Wine will have the item ID for blueberry. (Despite the name, it contains the item's ID rather than its ParentSheetIndex).

Create item instances
The ItemRegistry.Create method is the main way to construct items. For example:

Item pufferfish = ItemRegistry.Create("(O)128"); // can optionally specify count and quality
If the ID doesn't match a real item, the ItemRegistry will return an Error Item by default. You can override that by setting allowNull: true when calling the method.

You can also get a specific value type instead of Item if needed. This will throw a descriptive exception if the type isn't compatible (e.g. you try to convert furniture to boots).

Boots boots = ItemRegistry.Create<Boots>("(B)505"); // Rubber Boots
When creating an item manually instead, make sure to pass its ItemId (not QualifiedItemId) to the constructor. For example:

Item pufferfish = new Object("128", 1);
Work with item metadata
The ItemRegistry class provides several methods for working with item metadata. Some useful methods include:

method	effect
ItemRegistry.Create	Create an item instance.
ItemRegistry.Exists	Get whether a qualified or unqualified item ID matches an existing item. For example:
bool pufferfishExists = ItemRegistry.Exists("(O)128");
ItemRegistry.IsQualifiedId	Get whether the given item ID is qualified with the type prefix (like (O)128 instead of 128).
ItemRegistry.QualifyItemId	Get the unique qualified item ID given an unqualified or qualified one. For example:
string qualifiedId = ItemRegistry.QualifyItemId("128"); // returns (O)128
ItemRegistry.GetMetadata	Get high-level info about an item:
// get info about Rubber Boots
ItemMetadata metadata = ItemRegistry.GetMetadata("(B)505");

// get item ID info
$"The item has unqualified ID {metadata.LocalId}, qualified ID {metadata.QualifiedId}, and is defined by the {metadata.TypeIdentifier} item data definition.";

// does the item exist in the data files?
bool exists = metadata.Exists();
And get common parsed item data:

// get parsed info
ParsedItemData data = metadata.GetParsedData();
$"The internal name is {data.InternalName}, translated name {data.DisplayName}, description {data.Description}, etc.";

// draw an item sprite
Texture2D texture = data.GetTexture();
Rectangle sourceRect = data.GetSourceRect();
spriteBatch.Draw(texture, Vector2.Zero, sourceRect, Color.White);
And create an item:

Item item = metadata.CreateItem();
And get the type definition (note that this is very specialized, and you should usually use ItemRegistry instead to benefit from its caching and optimizations):

IItemDataDefinition typeDefinition = info.GetTypeDefinition();
ItemRegistry.ResolveMetadata	Equivalent to ItemRegistry.GetMetadata, except that it'll return null if the item doesn't exist.
ItemRegistry.GetData	Get the parsed data about an item, or null if the item doesn't exist. This is a shortcut for ItemRegistry.ResolveMetadata(id)?.GetParsedData(); see the previous method for info on the parsed data.
ItemRegistry.GetDataOrErrorItem	Equivalent to ItemRegistry.GetData, except that it'll return info for an Error Item if the item doesn't exist (e.g. for drawing in inventory).
ItemRegistry.GetErrorItemName	Get a translated Error Item label.
Define custom item types
You can implement IItemDataDefinition for your own item type, and call ItemRegistry.AddTypeDefinition to register it. This provides all the logic needed by the game to handle the item type: where to get item data, how to draw them, etc.

This is extremely specialized, and multiplayer compatibility is unknown. Most mods should add custom items within the existing types instead.

Locations

Get all locations

The list of root locations is stored in Game1.locations, but constructed building interiors aren't included. Instead, use the method Utility.ForAllLocations

Utility.ForAllLocations((GameLocation location) =>
{
   // do things with location here.
});
Note that farmhands in multiplayer can't see all locations; see GetActiveLocations instead.

Edit a location map

Position

A Character's position indicates the Character's coordinates in the current location.

Position Relative to the Map

Each location has an ``xTile`` map where the top-left corner of the map is (0, 0) and the bottom-right corner of the map is (location.Map.DisplayWidth, location.Map.DisplayHeight) in pixels.

There are two ways to get a Character's position in the current location: by absolute position and by tile position.

Position.X and Position.Y will give the XY coordinates in pixels.

getTileX() and getTileY() will give the XY coordinates in tiles.

Each tile is 64x64 pixels as specified by Game1.tileSize. The conversion between absolute and tile is as follows:

// Absolute position => Tile position
Math.Floor(Game1.player.Position.X / Game1.tileSize)
Math.Floor(Game1.player.Position.Y / Game1.tileSize)

// Tile position => Absolute position
Game1.player.getTileX() * Game1.tileSize
Game1.player.getTileY() * Game1.tileSize

// Tilemap dimensions
Math.Floor(Game1.player.currentLocation.Map.DisplayWidth / Game1.tileSize)
Math.Floor(Game1.player.currentLocation.Map.DisplayHeight / Game1.tileSize)
Position Relative to the Viewport

The viewport represents the visible area on the screen. Its dimensions are Game1.viewport.Width by Game1.viewport.Height in pixels; this is the same as the game's screen resolution.

The viewport also has an absolute position relative to the map, where the top-left corner of the viewport is at (Game1.viewport.X, Game1.viewport.Y).

The player's position in pixels relative to the viewport is as follows:

Game1.player.Position.X - Game1.viewport.X
Game1.player.Position.Y - Game1.viewport.Y
NPC

Creating Custom NPCs

Adding new NPCs involves editing a number of files:

New file: Characters\Dialogue\<name>.json (See also Modding:Event data)
New file: Characters\schedules\<name>.json (Note that the "s" in the "schedules" folder is lowercase!)
New file: Portraits\<name>.png
New file: Characters\<name>.png
Add entries Data\EngagementDialogue for NPCs that are marriable
Add entry to Data\NPCDispositions
Add entry to Data\NPCGiftTastes
Add entries to Characters\Dialogue\rainy
Add entries to Data\animationDescriptions (if you want custom animations in their schedule)
All of the above can be done with an AssetRequested event or Content Patcher. If you did all of this correctly, the game will spawn the NPC in for you. (If you didn't, it swallows the error)

User-interface (UI)

The User-interface (UI) is a collection of separate elements which make up the HUD and occasional popups.

Banner message

HUDMessage are those popups in the lower left hand screen. They have several constructors, which we will briefly go over here (a few non-relevant constructors are not included):

  public HUDMessage(string message);
  public HUDMessage(string message, int whatType);
  public HUDMessage(string type, int number, bool add, Color color, Item messageSubject = null);
  public HUDMessage(string message, string leaveMeNull)
  public HUDMessage(string message, Color color, float timeLeft, bool fadeIn)
So before we go over when you'd use them, I'm going to briefly note how the class HUDMessage uses these. (I encourage people to read the class if they have further questions, but I doubt most of us will need to know more than this)

Types available:

Achievement (HUDMessage.achievement_type)
New Quest (HUDMessage.newQuest_type)
Error (HUDMessage.error_type)
Stamina (HUDMessage.stamina_type)
Health (HUDMessage.health_type)
Color: Fairly obvious. It should be noted that while the first two don't give an option (they default to Color:OrangeRed), the fourth with the param 'leaveMeNull' displays as the same color as the game text.

For specifics:

public HUDMessage(string type, int number, bool add, Color color, Item messageSubject = null); - This allows for expanded customization of the message. More often used for money.
public HUDMessage(string message, string leaveMeNull) - Also displays no icon.
Not only displaying no icon, this type of HUDMessage does not have the square on the left side; it will draw a simple rectangle with text within.
public HUDMessage(string message, Color color, float timeLeft, bool fadeIn) - Displays a message that fades in for a set amount of time.
Note: For those of you who want a custom HUDMessage: - Almost all of these variables are public, excluding messageSubject, so feel free to customize! To modify them make a HUDMessage variable like so : HUDMessage <name> = new HUDMessage(<message>) ,now you can even animate them with code!

For example: add a new HUDMessage to show Error-image-ingame.png toaster popup.

Game1.addHUDMessage(new HUDMessage("MESSAGE", 3));
Another example: add a HUDMessage that shows up like a simple rectangle with no icon, and no square for the icon:

Game1.addHUDMessage(new HUDMessage("MESSAGE", ""));  // second parameter is the 'leaveMeNull' parameter
Active clickable menu

An active clickable menu is a UI drawn over everything else which accepts user input. For example, the game menu (shown in-game when you hit ESC or controller B) is an active clickable menu. The menu is stored in Game1.activeClickableMenu; if that field has a non-null value, the menu will be drawn and receive input automatically.

Each menu is different, so you need to look at the menu code to know how to interact with it. Since mods often need to get the current tab on the game menu, here's an example which handles the map tab:

if (Game1.activeClickableMenu is GameMenu menu)
{
  // get the tab pages
  IList<IClickableMenu> pages = this.Helper.Reflection.GetField<List<IClickableMenu>>(menu, "pages").GetValue();

  // option A: check tab ID
  if (menu.currentTab == GameMenu.mapTab)
  {
     ...
  }

  // option B: check page type
  switch (pages[menu.currentTab])
  {
    case MapPage mapPage:
       ...
       break;
  }
}
To create a custom menu, you need to create a subclass of IClickableMenu and assign it to Game1.activeClickableMenu. At its most basic, a menu is basically just a few methods you override (usually draw and receiveLeftClick at a minimum). When draw is called, you draw whatever you want to the screen; when receiveLeftClick is called, you check if it's within one of the clickable areas and handle it. Normally you'd use some convenience classes like ClickableTextureButton (which has a texture and position, and simplifies checking if they were clicked), though that's not strictly necessary. Here's a simple menu you can use as an example, which draws the birthday menu for Birthday Mod.

Assets
Farm type data
You can define custom farm types by editing the Data/AdditionalFarms asset.

This consists of a list of models, where each model has the fields listed below.

field	description
ID	A unique string ID for the farm type.
TooltipStringPath	The translation key containing the translatable farm name and description. For example, Strings/UI:Farm_Description will get it from the Farm_Description entry in the Strings/UI file.
The translated text must be in the form "<name>_<description>", like "Pineapple Farm_A farm shaped like a pineapple".

MapName	The asset name for the farm's map asset, relative to the Maps folder. For example, Farm_Pineapple would load Maps/Farm_Pineapple.
IconTexture	(Optional) The asset name for a 22x20 pixel icon texture, shown on the 'New Game' and co-op join screens.
WorldMapTexture	(Optional) The asset name for a 131x61 pixel texture that's drawn over the farm area in the in-game world map.
ModData	(Optional) The mod data fields for this farm type, which can be accessed in C# code via Game1.GetFarmTypeModData(key).
Farm map
The farm map contains the general appearance and layout of your farm. Modding:Maps describes the basic process of creating a map.

Copying and editing an existing farm map is recommended to avoid problems with missing information. The map must be added to the game files, and not replace an existing one.

Farm map properties
You can customize the farm behavior by setting map properties in the map asset.

When testing map property changes, it's best to create a new save since some of these are only applied when the save is created. These properties are optional, and the game will use default values for any properties that aren't specified.

Some notable map properties are:

Warp & map positions set the player position when arriving on the farm (e.g. BackwoodsEntry when arriving from the backwoods, or WarpTotemEntry when using a warp totem or farm obelisk), and the default positions of some location contents (e.g. MailboxLocation for the default mailbox position).
Farmhouse interior properties set the appearance and contents of the farmhouse (e.g. FarmHouseFlooring for the default flooring, or FarmHouseStarterGift for what's in the starter giftbox).
Fishing properties override fishing and crab pot behavior.
Plants, forage, & item spawning properties override how crops, forage, artifact spots, etc work on the farm.
Location data
Optionally, you can override additional farm location behavior by editing Data/Locations. Each farm type can have its own entry, with a key in the form Farm_<farm type ID>. If omitted, it defaults to the location data for the standard farm layout.

This can be used to override forage, fish, crab pot catches, artifact spots, etc.

For custom farms, some fields should have specific values to preserve expected behavior:

field	description
DisplayName	A tokenizable string for the farm name. It should contain at least the FarmName token to be sure the farm name is shown. The standard value is [LocalizedText Strings\\StringsFromCSFiles:MapPage.cs.11064 [EscapedText [FarmName]]].
CreateOnLoad	Must be null or omitted. Any other value will create duplicate locations.
CanPlantHere	Should be true or omitted. If false, crops can't be grown on your farm.
Example
For example, this Content Patcher pack adds a farm type with custom location data.

{
    "Changes": [
        // add farm type
        {
            "Action": "EditData",
            "Target": "Data/AdditionalFarms",
            "Entries": {
                "{{ModId}}_PineappleFarm": { // for technical reasons, you need to specify the ID here *and* in the 'ID' field
                    "ID": "{{ModId}}_PineappleFarm",
                    "TooltipStringPath": "Strings/UI:{{ModId}}",
                    "MapName": "{{ModId}}",
                    "IconTexture": "Mods/{{ModId}}/Icon",
                    "WorldMapTexture": "Mods/{{ModId}}/WorldMap"
                }
            }
        },

        // add farm name + description
        {
            "Action": "EditData",
            "Target": "Strings/UI",
            "Entries": {
                "{{ModId}}": "Pineapple Farm_A farm shaped like a pineapple!" // tip: use {{i18n}} to translate it
            }
        },

        // load map
        {
            "Action": "Load",
            "Target": "Maps/{{ModId}}",
            "FromFile": "assets/map.tmx"
        },

        // load icon
        {
            "Action": "Load",
            "Target": "Mods/{{ModId}}/Icon, Mods/{{ModId}}/WorldMap",
            "FromFile": "assets/{{TargetWithoutPath}}.png"
        },

        // custom location data
        {
            "Action": "EditData",
            "Target": "Data/Locations",
            "Entries": {
                "Farm_{{ModId}}_PineappleFarm": {
                    "DisplayName": "[LocalizedText Strings\\StringsFromCSFiles:MapPage.cs.11064 [EscapedText [FarmName]]]",
                    "CanPlantHere": true,
                    "DefaultArrivalTile": {"X": 64, "Y": 15},
                    "MinDailyWeeds": 5,
                    "MaxDailyWeeds": 11,
                    "ArtifactSpots": [
                        // default artifact data
                        {
                            "Id": "Coal",
                            "ItemId": "(O)382",
                            "Chance": 0.5,
                            "MaxStack": 3
                        },
                        {
                            "Id": "MixedSeeds",
                            "ItemId": "(O)770",
                            "Chance": 0.1,
                            "MaxStack": 3
                        },
                        {
                            "Id": "Stone",
                            "ItemId": "(O)390",
                            "Chance": 0.25,
                            "MaxStack": 3
                        },
                        // custom artifacts
                        {
                            "Id": "SpringSeeds",
                            "ItemId": "(O)495",
                            "Chance": 0.2,
                            "MaxStack": 4,
                            "Condition": "SEASON Spring",
                            "Precedence": 1
                        },
                        {
                            "Id": "SummerSeeds",
                            "ItemId": "(O)496",
                            "Chance": 0.2,
                            "MaxStack": 4,
                            "Condition": "SEASON Summer",
                            "Precedence": 1
                        },
                        {
                            "Id": "FallSeeds",
                            "ItemId": "(O)497",
                            "Chance": 0.2,
                            "MaxStack": 4,
                            "Condition": "SEASON Fall",
                            "Precedence": 1
                        },
                        {
                            "Id": "WinterSeeds",
                            "ItemId": "(O)498",
                            "Chance": 0.2,
                            "MaxStack": 4,
                            "Condition": "SEASON Winter",
                            "Precedence": 1
                        }
                    ]
                }
            }
        }
    ]
}

DialogueBox

A DialogueBox is a text box with a slightly larger, slightly boldfaced text, with "typewriter-like" effect.

There are several variants, including ones with a dialogue/conversation choices.

Within the message, use a caret "^" to put a linebreak.

Here is an example of a simple, choiceless output:

using StardewValley.Menus;  // This is where the DialogueBox class lives

string message = "This looks like a typewriter ... ^But it's not ...^It's a computer.^";
Game1.activeClickableMenu = new DialogueBox(message);

To utilise options, you are better off using createQuestionDialogue.

private void SampleClick()
{
    // List of choices to give the farmer.
    List<Response> choices = new List<Response>()
            {
                new Response("dialogue_id1","Choice 1" ),
                new Response("dialogue_id2", "Choice 2"),
                new Response("dialogue_id3", "Choice 3"),
                new Response("dialogue_id4", "Choice 4")
            };

    // And here we case it to pop up on the farmer's screen. When the farmer has picked a choice, it sends that information to the method below (DialogueSet
    Game1.currentLocation.createQuestionDialogue($"What is the question?", choices.ToArray(), new GameLocation.afterQuestionBehavior(DialogueSet));
}

public void DialogueSet(Farmer who, string dialogue_id)
{
	// Here you get which option was picked as dialogue_id.
	Game1.addHUDMessage(new HUDMessage($"Farmer {who} chose option {dialogue_id}"));
    
}
Mail

If you are new to SMAPI or to modding Stardew Valley in general, sending a simple letter to the player's mailbox is a great place to start your learning journey. You will be treated to some simple to understand code and concepts, as well as receive some instant gratification in the form of a tangible, in-game letter that you can see in action.

Mail content

Before you can actually send any of your own custom mail to the player, you must decided how your letter will be composed. By that I mean, is your letter static - always the same text - or is it dynamic - text changes based on a variable piece of information? Obviously a static letter will be easier to implement, so if you are just starting off, go that route for now. However, both static and dynamic methods are explained below.

To send mail, whether static or dynamic, you first have to let Stardew Valley know about your content, also referred to as an asset. In the case of mail, you have to inject your additions into the mail data. You accomplish this via the IAssetEditor interface. You can implement IAssetEditor from your ModEntry class, or create a separate class that implements IAssetEditor to inject new mail content into "Data\Mail.xnb". The examples cited below use the latter approach for clarity, easy of reuse, and encapsulation:

Inject static content

Most times a static, predefined letter will suffice, whether you are including an attachment (i.e., object, money, etc.) or not. "Static" simply means you do not need to change the text once it is typed before sending the letter. A "static" letter will always be available in the game (unless you remove it from the mod or the mod is removed by the player) so that means the letter is still available if the player quits with your letter still in the mailbox and then returns to play later. This can be an issue with "dynamic" letters, as explained in more detail in that section, so use "static" content whenever possible.

You can softly reference the player's name, using "@", but other replace codes that may work in dialog texts, like %pet or %farm, do not work in static mail content at this time. However, you can make use of some special characters that display an icon in the letter, such as "=", which will display a purple star, "<", which will display a pink heart, the "$", which will be replaced with a gold coin, the ">", which will display a right arrow, the "`", which will display an up arrow, and the "+", which will display a head riding a skateboard (maybe?). There may be additional special cases as well that are not yet documented.

The example below adds 4 letters into the mail data collection. Note, that the code below does not send any letters to the player, but simply makes them available to Stardew Valley game so they can be sent.

using StardewModdingAPI;

namespace MyMod
{
    internal sealed class ModEntry: Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
        }
        
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
            {
                e.Edit(this.EditImpl);
            }
        }

        public void EditImpl(IAssetData asset)
        {
            var data = asset.AsDictionary<string, string>().Data;

            // "MyModMail1" is referred to as the mail Id.  It is how you will uniquely identify and reference your mail.
            // The @ will be replaced with the player's name.  Other items do not seem to work (''i.e.,'' %pet or %farm)
            // %item object 388 50 %%   - this adds 50 pieces of wood when added to the end of a letter.
            // %item tools Axe Hoe %%   - this adds tools; may list any of Axe, Hoe, Can, Scythe, and Pickaxe
            // %item money 250 601  %%  - this sends a random amount of gold from 250 to 601 inclusive.
            data["MyModMail1"] = "Hello @... ^A single carat is a new line ^^Two carats will double space.";
            data["MyModMail2"] = "This is how you send an existing item via email! %item object 388 50 %%";
            data["MyModMail3"] = "Coin $   Star =   Heart <   Dude +  Right Arrow >   Up Arrow `";
            data["MyWizardMail"] = "Include Wizard in the mail Id to use the special background on a letter";
        }
    }
}
Send a letter (using static content)

Now that you have your letter loaded, it's time to send it to the player. There are a couple different methods available to accomplish this as well, depending on your need. Two examples are shown below. The distinction between the two methods will be explained below:

    Game1.player.mailbox.Add("MyModMail1");
    Game1.addMailForTomorrow("MyModMail2");
The first method (Game1.player.mailbox.Add) adds the letter directly into the mailbox for the current day. This can be accomplished in your "DayStarting" event code, for example. Mail added directly to the mailbox is not "remembered" as being sent even after a save. This is useful in some scenarios depending on your need.

The second method (Game1.addMailForTomorrow) will, as the name implies, add the letter to the player's mailbox on the next day. This method remembers the mail (Id) sent making it possible not to send the same letter over and over. This can be handled in "DayStaring", "DayEnding" or other events, as dictated by your need.

You may be able to put the letter directly into the mailbox and also have it be remembered using the mailRecieved collection. You can simply add your mailId manually if you want it to be remembered when using the add directly to mailbox method.

If you want Stardew Valley to forget that a specific letter has already been sent, you can remove it from the mailReceived collection. You can iterate through the collection as well using a foreach should you need to remove mail en mass.

    Game1.player.mailReceived.Remove("MyModMail1");
That is all there is to sending a simple letter. Attaching objects and sending money via letter is straight-forward, but sending recipes is more complicated and will need some additional explanation at a future time.

Inject dynamic content

If you want to send a letter that contains data that needs to change based on the situation, such as the number of purple mushrooms eaten today, then you have to create that letter content each time you plan on sending it, especially if you want an up-to-date value. That is what I am referring to by "dynamic" letters.

Consider the following source code, which is basically an enhanced version of the static mail class shown above, that will also support "dynamic" content. You could certainly always use the enhanced version of this code and just not make use of the dynamic content unless needed. The code was separated for the purpose of illustrating the differences.

using StardewModdingAPI;
using System.Collections.Generic;

namespace MyMail
{
    internal sealed class ModEntry: Mod
    {
        // This collection holds any letters loaded after the initial load or last cache refresh
        private Dictionary<string, string> dynamicMail = new();
       
        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
             if (e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
                 e.Edit(this.EditImpl);
        }

        public void EditImpl(IAssetData asset)
        {
            var data = asset.AsDictionary<string, string>().Data;

            // This is just an example
            data["StaticMail"] = "If there were any letters with static content they could be placed here.";

            // Inject any mail that was added after the initial load.
            foreach (var item in dynamicMail)
            {
                data.Add(item);
            }

            dynamicMail.Clear();    // For the usage of this MOD the letters are cleared
        }

        /// <summary>
        /// Add a new mail asset into the collection so it can be injected by the next cache refresh.  The letter will
        /// not be available to send until the cache is invalidated in the code.
        /// </summary>
        /// <param name="mailId">The mail key</param>
        /// <param name="mailText">The mail text</param>
        public void Add(string mailId, string mailText)
        {
            if (!string.IsNullOrEmpty(mailId))
            {
                dynamicMail[mailId] = mailText;
            }
        }
    }
}
You will notice that there is really very little difference in the code used for static mail and dynamic mail. The class that supports dynamic mail has a private dictionary collection for holding on to any mail content waiting to be injected. It could have been made public to allow mail to be added directly into the collection, but that is not good practice. Instead a public Add method was provided so that mail could be sent, so to speak, to the collection. This code is for a specific MOD, not a robust framework, so it isn't overly concerned with error handling. You can improve that based on your needs.

Notice the additional code in the Edit method, where any mail in the dynamicMail collection is injected into Stardew Valley's content. There will be no mail in the dynamicMail collection when the MOD is loaded (in this case) the first time. If you add mail after the original load, then the content will have to be reloaded by invalidating the cache. Refer to Cache invalidation for more details.

Send a letter (using dynamic content)

You can hook into other events, such as "Day Starting" or "Day Ending" to generate the letter you want to send. Consider this simple example, that is only for illustration purposes.

    private void OnDayStarting(object sender, DayStartedEventArgs e)
    {
        string mailMessage = $"@, you have gathered {Game1.stats.rabbitWoolProduced} units of rabbit wool!";

        mailData.Add("MyModMailWool", mailMessage);      // Add this new letter to the mail collection (for next refresh).

        Game1.mailbox.Add("MyModMailWool");              // Add to mailbox and we don't need to track it

        this.Helper.GameContent.InvalidateCache("Data\\mail"); // note that as of SMAPI 3.14.0, this only invalidates the English version of the asset. 
    }
This example formats a letter, showing the up-to-date count of rabbit wool, makes it available to the mail collection, places that letter in the mailbox, and then invalidates the cache so that this new letter will be injected during the cache refresh. In this case there is no need to remember mailId as the letter will be recreated each time it needs to be sent, which in this example is everyday. Again, this code is only for illustration of the concept.

There is an important caveat to understand when injecting mail in this simple fashion. The various mail frameworks available handle this issue, and this section will be expanded to explain how to overcome the issue, but it is being covered here to ensure you have a complete understanding of how MODs work with Stardew Valley and SMAPI.

If you add a dynamic letter and inject it into the content at Day Ending, you have to add the mail for display tomorrow obviously. That means the game will be saved with a reference to the dynamic letter ("MyMailModWool" in this example) pending in the mail box. If the player quits the game at that point and returns later to continue playing, then that dynamic letter is not available, resulting in a "phantom letter". The mailbox will show a letter is available but when clicked on nothing will display. This can be handled in several ways, including by saving the custom letters and loading them when the player continues, but again this example code does not cover that yet. That is why the example uses On Day Starting and makes the letter available right away.

Other

Add a small animation

location.temporarySprites.Add(new TemporaryAnimatedSprite(...))
See TemporaryAnimatedSprite for more details

Play a sound

location.playSound("SOUND");
(e.g., "junimoMeep1")

General concepts

Time format

The in-game time of day is tracked using a version of 24-hour format informally called "26-hour time", measured in 10-minute intervals. This is the format used by Game1.timeOfDay in a C# mod or {{Time}} in a Content Patcher pack.

Sample times:

time value	display text
600	6:00 am
1250	12:50 am
1300	1:00 pm
2600	2:00 am (before sleeping)
The internal time will continue incrementing forever until you sleep (e.g. 6am the next day would be 3000 in that case).

Tiles

The world is laid out as a grid of tiles. Each tile has an (x, y) coordinate which represents its position on the map, where (0, 0) is the top-left tile. The x value increases towards the right, and y increases downwards. For example:

Modding - creating an XNB mod - tile coordinates.png

You can use the Debug Mode mod to see tile coordinates in-game.

Positions

The game uses three related coordinate systems:

coordinate system	relative to	notes
tile position	top-left corner of the map	measured in tiles; used when placing things on the map (e.g., location.Objects uses tile positions).
absolute position	top-left corner of the map	measured in pixels; used when more granular measurements are needed (e.g., NPC movement).
screen position	top-left corner of the visible screen	measured in pixels; used when drawing to the screen.
Here's how to convert between them (there are also helpful methods in Utility for some of these):

conversion	formula
absolute	→	screen	x - Game1.viewport.X, y - Game1.viewport.Y
absolute	→	tile	x / Game1.tileSize, y / Game1.tileSize
screen	→	absolute	x + Game1.viewport.X, y + Game1.viewport.Y
screen	→	tile	(x + Game1.viewport.X) / Game1.tileSize, (y + Game1.viewport.Y) / Game1.tileSize
tile	→	absolute	x * Game1.tileSize, y * Game1.tileSize
tile	→	screen	(x * Game1.tileSize) - Game1.viewport.X, (y * Game1.tileSize) - Game1.viewport.Y
Zoom level

The player can set an in-game zoom level between 75% and 200%, which adjusted the size of all pixels shown on the screen. For example, here's a player with the same window size at different zoom levels:

min zoom level (75%)	max zoom level (200%)
Zoom level 75.png	Zoom level 200.png
Effect on SMAPI mods
In game code, this is represented by the Game1.options.zoomLevel field. Coordinates are generally adjusted for zoom level automatically, so you rarely need to account for this; but you can convert an unadjusted coordinate using position * (1f / Game1.options.zoomLevel) if needed.
UI scaling

The player can scale the UI between 75% and 150%, separately from and alongside the zoom level. That adjusts the size of pixels shown on the screen for UI elements only. For example, here's a player with the same window size at different UI scaling levels:

min UI scale (75%)	max UI scale (150%)
UI scale 75.png	UI scale 150.png
Effect on SMAPI mods
The game has two distinct scaling modes depending on the context: UI mode and non-UI mode. You can check Game1.uiMode to know which mode is active. You should be careful not to mix UI and non-UI coordinates to avoid tricky calculations; for example, do all your work in one coordinate system and then convert them once.
A quick reference of common scenarios:
context	scaling mode which applies
clickable menus	UI mode (usually)
HUD elements	UI mode
RenderingActiveMenu
RenderedActiveMenu	UI mode
Rendering
Rendered	depends on the context; check Game1.uiMode
draw method for world objects	non-UI mode
tile (non-pixel) coordinates	not affected by UI scaling
If you need to draw UI when the game isn't in UI mode, you can explicitly set UI scaling mode:
Game1.game1.InUIMode(() =>
{
   // your UI draw code here
});
In UI mode, you should usually replace Game1.viewport with Game1.uiViewport. Don't do this if you'll adjust the positions for UI scaling separately, since double-conversion will give you incorrect results. You can convert between UI and non-UI coordinates using Utility.ModifyCoordinatesForUIScale and Utility.ModifyCoordinatesFromUIScale.
You can test whether your mod accounts for this correctly by setting the zoom to maximum and the UI scale to minimum (i.e., have them at opposite values) or vice versa; in particular check any logic which handles pixel positions, like menus clicking.
Multiplayer concepts for C# mods

Net fields

A 'net type' is any of several classes which Stardew Valley uses to sync data between players, and a 'net field' is any field or property of those types. They're named for the Net prefix in their type names. Net types can represent simple values like NetBool, or complex values like NetFieldDictionary. The game will regularly collect all the net fields reachable from Game1.netWorldState and sync them with other players. That means that many mod changes will be synchronised automatically in multiplayer.

Although net fields can be implicitly converted to an equivalent value type (like bool x = new NetBool(true)), their conversion rules are counterintuitive and error-prone (e.g., item?.category == null && item?.category != null can both be true at once). To avoid bugs, never implicitly cast net fields; access the underlying value directly instead. The build config NuGet package should detect most implicit conversions and show an appropriate build warning.

The following describes the upcoming SMAPI 1.6.9, and may change before release.

The game no longer has implicit conversion operators for net fields in 1.6.9.

Here's how to access the data in some common net types:

net type	description
NetBool
NetColor
NetFloat
NetInt
NetPoint
NetString	A simple synchronised value. Access the value using field.Value.
NetCollection<T>
NetList<T>
NetObjectList<T>	A list of T values. This implements the standard interfaces like IEnumerable<T> and IList<T>, so you can iterate it directly like foreach (T value in field).
NetLongDictionary<TValue, TNetValue>
NetPointDictionary<TValue, TNetValue>
NetVector2Dictionary<TValue, TNetValue>	Maps Long, Point, or Vector2 keys to instances of TValue (the underlying value type) and TNetValue (the synchronised net type). You can iterate key/value pairs like foreach (KeyValuePair<Long, TValue> pair in field.Pairs) (replacing Long with Point or Vector2 if needed).
Farmhand shadow world

In multiplayer, secondary players (farmhands) don't see most of the in-game locations. Instead their game creates a single-player copy of the world before they join, and then only fetches the farm area and their current location (called active locations) from the host player. The unsynchronized locations often don't match what players within those locations see.

This has some significant implications for C# mods:

The Game1.locations list shows both active and shadow locations. While mods can access the shadow locations, these don't reflect the real data on the server and any changes to them won't be synced to the host.
There may be duplicate copies of NPCs, horses, etc in the shadow world. Only those in active locations are 'real'.
Game methods (like Game1.getCharacterByName) may not correctly distinguish between the 'real' and 'shadow' copies.
When a farmhand warps to a location, the game fetches the real location from the host player before the warp completes. For a short while, the farmhand may have a null currentLocation field while they're between locations.
You can check whether a location is active using its IsActiveLocation method:

foreach (GameLocation location in Game1.locations)
{
    if (!location.IsActiveLocation())
        continue; // shadow location

    ...
}
Main classes

Game1

Game1 is the game's core logic. Most of the game state is tracked through this class. Here are some of the most useful fields:

field	type	purpose
Game1.player	Farmer	The current player.
Game1.currentLocation	GameLocation	The game location containing the current player. For a non-main player, may be null when transitioning between locations.
Game1.locations	IList<GameLocation>	All locations in the game. For a non-main player, use SMAPI's GetActiveLocations method instead.
Game1.timeOfDay
Game1.dayOfMonth
Game1.currentSeason
Game1.year	int
int
string
int	The current time, day, season, and year. See also SMAPI's date utility.
Game1.itemsToShip	IList<Item>	Do not use (this is part of the save logic). See Game1.getFarm().getShippingBin(Farmer) instead.
Game1.activeClickableMenu	IClickableMenu	The modal menu being displayed. Creating an IClickableMenu subclass and assigning an instance to this field will display it.
GameLocation et al

GameLocation represents an in-game location players can visit. Each location has a map (the tile layout), objects, trees, characters, etc. Here are some of the most useful fields for any location:
field	type	purpose
Name	string	The unique name for this location. (This isn't unique for constructed building interiors like cabins; see uniqueName instead.)
IsFarm	bool	Whether this is a farm, where crops can be planted.
IsGreenhouse	bool	Whether this is a greenhouse, where crops can be planted and grown all year.
IsOutdoors	bool	Whether the location is outdoors (as opposed to a greenhouse, building, etc).
characters	NetCollection of NPC	The villagers, pets, horses, and monsters in the location.
critters	List of Critter	The temporary birds, squirrels, or other critters in the location.
debris	NetCollection of Debris	The floating items in the location.
farmers	FarmerCollection	The players in the location.
Objects	OverlaidDictionary	The placed fences, crafting machines, and other objects in the current location. (OverlaidDictionary is basically a NetVector2Dictionary with logic added to show certain quest items over pre-existing objects.)
terrainFeatures	NetVector2Dictionary of TerrainFeature	The trees, fruit trees, tall grass, tilled dirt (including crops), and flooring in the location. For each pair, the key is their tile position and the value is the terrain feature instance.
waterTiles	bool[,]	A multi-dimensional array which indicates whether each tile on the map is a lake/river tile. For example, if (location.waterTiles[10, 20]) checks the tile at position (10, 20).
BuildableGameLocation is a subclass of GameLocation for locations where players can construct buildings. In the vanilla game, only the farm is a buildable location. Here are the most useful fields:
field	type	purpose
buildings	NetCollection of Building	The buildings in the location.
Farm is a subclass of both GameLocation and BuildableGameLocation for locations where the player can have animals and grow crops. In the vanilla, there's only one farm location (accessed using Game1.getFarm()). Here are its most useful properties:
field	type	purpose
animals	NetLongDictionary of FarmAnimal	The farm animals currently in the location.
resourceClumps	NetCollection of ResourceClump	The giant crops, large stumps, boulders, and meteorites in the location.
piecesOfHay	NetInt	The amount of hay stored in silos.
shippingBin	NetCollection of Item	The items in the shipping bin.
There are a number of subclasses for specific location (like AdventureGuild) which have fields useful for specific cases.


Raw data

Events are stored according to the location they occur in, in the asset Data/Events/<LocationName>. You can unpack your XNB files for reference.

For example, here's the raw data (in English) for events in Pam's trailer (Data/Events/Trailer) as of 1.6.14:

ExpandData 
{
  "35/f Penny 1000/p Penny": "50s/-1000 -1000/farmer -30 30 0 Penny 12 7 0 Pam -100 -100 0/skippable/specificTemporarySprite pennyMess/viewport 12 7 true/pause 1000/speak Penny \"Ughh... It's so dirty in here.$s\"/pause 500/warp farmer 12 9/playSound doorClose/pause 500/faceDirection Penny 2/pause 500/emote Penny 16/pause 300/speak Penny \"@! Um... Sorry that it's such a mess. I was about to clean up.$u\"/pause 500/move farmer 0 -1 0/pause 600/emote Penny 32/pause 300/speak Penny \"You'll help me? You really mean it?$h\"/pause 500/faceDirection Penny 1/faceDirection farmer 1/speak Penny \"Okay, you can get started over there. I'll clean the kitchen.\"/move farmer 2 0 1/move farmer 0 -2 1/move farmer 1 0 1/move Penny -1 0 0/animate Penny false true 100 24 25/animate farmer false true 100 35/pause 200/playSound dwop/removeSprite 16 6/pause 200/move Penny -1 0 0/animate Penny false true 100 24 25/stopAnimation farmer/faceDirection farmer 0/pause 1200/stopAnimation Penny/removeSprite 10 5/playSound dwop/move Penny -1 0 0/animate Penny false true 100 24 25/pause 900/animate farmer false true 100 41/pause 300/playSound dwop/removeSprite 15 5/stopAnimation farmer/pause 1400/stopAnimation Penny/move Penny 1 0 0/pause 800/warp Pam 12 9/playSound doorClose/stopMusic/move Pam 0 -1 0/faceDirection Penny 2/faceDirection farmer 3/faceDirection Pam 1/pause 500/faceDirection Pam 3/speak Pam \"Whaddya think you're doing?!$u\"/faceDirection Pam 1/faceDirection Pam 3/faceDirection Pam 0/speak Pam \"Stop it! I had everything just the way I like it!$u\"/move Penny 2 0 2/pause 500/emote Penny 28/pause 500/speak Penny \"Mom, the house is a total mess. @ and I were just trying to tidy things up a bit.#$b#*sniff* *sniff*... Were you at the saloon just now? You smell like beer...$s\"/pause 300/move Pam -1 0 3/emote Pam 12/pause 400/speak Pam \"It's none of your damn business where I go!$4\"/pause 500/speak Penny \"It IS my business! I don't want you destroying yourself!$a#$b#Don't you realize your choices have an effect on me? Stop being so selfish!$a\"/faceDirection Pam 0/shake Pam 5000/pause 600/speak Pam \"Selfish? I put a roof over your head and clothes on your back and you call me selfish!? You ungrateful little...$u\"/pause 500/emote farmer 28/pause 500/faceDirection Pam 1/faceDirection Penny 1/move farmer -2 0 2/move farmer 0 2 3/pause 500/speak Pam \"You'd better go. I'm sorry you had to see this, kid.$4\"/pause 500/move farmer -1 0 2/move farmer 0 1 2/pause 500/faceDirection farmer 0/faceDirection Penny 2/pause 700/faceDirection farmer 2/pause 500/warp farmer -40 -40/playSound doorClose/pause 500/move Pam 0 -1 1/pause 300/faceDirection Penny 3/speak Pam \"He's a nice young man...^She's a nice young lady...\"/speak Pam \"But I don't want you tellin' others to clean up my house! It's embarrassing! You understand?$4\"/pause 300/faceDirection Penny 2/pause 600/showFrame Penny 23/pause 700/speak Penny \"...Yes, mother.$s\"/pause 1000/mail PennyCleanTrailer/end warpOut",
  "36/f Penny 1500/p Penny": "musicboxsong/9 7/farmer -30 30 0 Penny 9 7 0/skippable/pause 1000/playSound doorClose/warp farmer 12 9/pause 400/faceDirection Penny 2/pause 300/speak Penny \"@, you came at a good time!#$b#I'm just about finished cooking a new recipe I invented!$h\"/pause 400/move farmer 0 -2 3/faceDirection Penny 1/move farmer -2 0 3/speak Penny \"Let me just finish up real quick.\"/pause 200/faceDirection Penny 0/pause 200/animate Penny false true 120 29 30/playSound crafting/pause 320/playSound crafting/pause 320/playSound crafting/pause 320/playSound crafting/pause 320/stopAnimation Penny/pause 500/playSound openBox/showFrame Penny 29/pause 400/playSound furnace/showFrame Penny 30/pause 1500/playSound clank/faceDirection Penny 0/pause 500/faceDirection Penny 1 true/showFrame Penny 28/speak Penny \"Here, give that a little taste.\"/pause 800/faceDirection farmer 2/farmerEat 200/showFrame Penny 4/pause 2500/stopAnimation farmer/pause 500/playSound gulp/animate farmer false true 350 104 105/pause 500/specificTemporarySprite pennyCook/pause 1500/faceDirection farmer 2/pause 500/speak Penny \"$q 72 null#...well?#$r 72 50 event_cook1#(Lie) Mmm! That was delicious!#$r 73 -50 event_cook2#Uh... can I get the rest to go?#$r 73 0 event_cook3#Well it's definitely unique... how did you get it so rubbery?\"/pause 500/speak Penny \"$p 72#Hey, since you're the first person to try it, I'm going to name this one 'Chili de @'.$h|Well, I guess this recipe was a failure...$s\"/stopAnimation farmer/faceDirection farmer 3/pause 600/speak Penny \"Um... so how about we watch a movie or something?\"/pause 500/move farmer 5 0 0 true/move Penny 5 0 0 true/globalFade/viewport -1000 -1000/end dialogue Penny \"Thanks for being my taste-tester.$h\"",
  "963313/n pamPotatoJuice": "playful/10 7/farmer -100 -100 0 Pam 10 7 0/mail pamNewChannel/skippable/pause 3000/speak Pam \"Heheh... I see the delivery came in.$h\"/pause 500/speak Pam \"Let's have a little taste.$h\"/pause 1000/playSound coin/showFrame Pam 32/pause 2000/animate Pam false true 400 33 34/playSound glug/pause 800/playSound glug/pause 800/playSound glug/pause 800/playSound glug/pause 790/stopAnimation Pam/showFrame Pam 32/pause 400/emote Pam 40/pause 800/showFrame Pam 35/textAboveHead Pam \"Ptooey!\"/playSound slimedead/shake Pam 2000/pause 2000/animate Pam false true 400 28 29/speak Pam \"I said potato, not fermented baboon kidney!$u\"/pause 2000/end dialogue Pam \"The juice? Yeah, I tried it, kid. $4#$b#How'd it taste? ...Some things are better left unsaid. Let's just leave it at that.$4\""
}
Event preconditions

Format

Each event has a key in the form <event ID>/[preconditions], which has two components:

field	usage
event ID	A unique string ID which identifies the event. Older vanilla events use a number for legacy reasons, but mod events are strongly encouraged to follow the unique string ID format (including the mod ID prefix) to prevent conflicts between mods.
preconditions	
A slash (/)-delimited list of the preconditions listed in the sections below. You can prefix any precondition with ! to negate it (e.g. !Season Winter for any season except winter). If an event has no preconditions, it must have a trailing slash to distinguish it from forks.

Precondition keys are case-insensitive, but their arguments may not be. Using the exact capitalization shown is recommended to avoid issues.

You can escape spaces and slashes in arguments using quotes like this: /GameStateQuery "SEASON Spring"/.

For example (assuming you use Content Patcher with its {{ModId}} token):

{{ModId}}_EventId/: event ID is {{ModId}}_EventId with no preconditions.
{{ModId}}_EventId/Time 1900 2300/Friendship Clint 750: event ID is {{ModId}}_EventId; the event only happens between 7pm and 11pm when you have 3 hearts (750 points) with Clint.
Short aliases

Some older preconditions have a short-form alias, like j for DaysPlayed. These are deprecated, case-sensitive, and shouldn't be used in new events.

Built-in preconditions

Game state queries

This lets you check arbitrary conditions not covered by another precondition.

name and arguments	alias	description
GameStateQuery <query>	G	A game state query matches, like G !WEATHER Here Sun for 'not sunny in this location'.
World/Context

These check the current time, date, weather, etc. They're not player-specific.

name and arguments	alias	description
ActiveDialogueEvent <ID>		The special dialogue event with the given ID (including Conversation Topics) is in progress.
DayOfMonth <number>+	u	Today is one of the specified days of the month (may specify multiple days). Days should be integers (e.g., 12).
DayOfWeek <day>+		Today is one of the specified days (may specify multiple days). This can be a case-insensitive three-letter abbreviation (like Mon) or full name (like Monday).
FestivalDay		Today is a festival day.
GoldenWalnuts <number>	N	The players in total have found at least this many golden walnuts, including spent walnuts.
InUpgradedHouse [level]	L	The current location is a farmhouse or cabin, and it has been upgraded at least [level] times. Default value: 2.
NPCVisible <name>	v	The NPC with that internal name is present and visible in any location.
NpcVisibleHere <name>	p	The NPC with that internal name is present and visible in the current location.
Random <number>	r	Matches randomly, where <number> is the probability between 0 and 1 of permitting the event (e.g., 0.2 for a 20% chance).
Season <season>+		The current season is one of the given values (may specify multiple seasons).
Time <min> <max>	t	The current time of day is between the given values, inclusive. Values use the 26-hour clock (600 to 2600).
UpcomingFestival <number>		A festival day will occur within the given number of days.
Weather <weather>	w	The weather in the current location's context matches <weather>. Valid values: rainy, sunny, or a specific weather ID.
WorldState <ID>	*	The given world state ID is active anywhere.
Year <year>	y	If <year> is 1, must be in the first year. Otherwise, year must be at least this value.
Current player

These check the current player (the one playing this instance of the game).

name and arguments	alias	description
ChoseDialogueAnswers <dialogue ID>+	q	Current player has chosen all of the given dialogue answer IDs (can specify multiple IDs).
Dating <name>	D	Current player is dating the NPC with that internal name.
EarnedMoney <number>	m	Current player has earned at least this much money (including spent money).
FreeInventorySlots <number>	c	Current player has at least this many free inventory slots.
Friendship <name> <number>+	f	Current player has at least this many friendship points with all of the NPCs with those internal names (can specify multiple name/number pairs).
Gender <gender>	g	Current player is male (if <gender> is male, case-insensitive) or not male (if <gender> is anything else).
HasItem <item ID>	i	Current player has the given item in their inventory.
HasMoney <number>	M	Current player has at least this much money on hand (does not include spent money).
LocalMail <letter ID>	n	Current player has received the given mail.
MissingPet [pet]	h	Current player has not yet received a pet, and their preference matches [pet] (can be any pet type). Default: matches any preference.
ReachedMineBottom [number]	b	Current player has reached the bottom floor of the Mines at least this many times. Default value: 1.
Roommate	R	Current player is roommates with any NPC.
SawEvent <event ID>+	e	Current player has seen any of the given events (may specify multiple IDs).
To check whether the player has seen all of several events, use this precondition multiple times.

SawSecretNote <number>	S	Current player has seen the Secret Note with the given (integer) ID.
Shipped <item ID> <number>+	s	Current player has shipped at least this many of each specified item (can specify multiple item/number pairs). This only works for the items tracked by the game for shipping stats (shown in the shipping collections menu).
Skill <name> <level>		Current player has reached at least this level in the given skill (one of Combat, Farming, Fishing, Foraging, Luck, or Mining)
Spouse <name>	O	Current player is married or engaged to the NPC with that internal name.
SpouseBed	B	Current player has a double bed in their house (or, if they have a roommate, a single bed). But if the roommate is Krobus, will never match.
Tile <x> <y>+	a	Current player is standing on one of the given tile positions (can specify multiple x/y positions). This works even if the player is not currently warping.
Host player

These check the host player (the one running a multiplayer farm, not necessarily the current player). If single-player, this is always the current player.

name and arguments	alias	description
CommunityCenterOrWarehouseDone	C	The Community Center or Joja Warehouse has been completed.
DaysPlayed <number>	j	Host player has played at least this many days.
HostMail <letter ID>	Hn	Host player has received the indicated mail.
HostOrLocalMail <letter ID>	*n	Either the host or the current player has received the indicated mail.
IsHost	H	Current player is the host player.
JojaBundlesDone	J	All Joja bundles have been completed.
Deprecated

These are deprecated and should no longer be used in newer events.

Expanddeprecated preconditions 
name and arguments	alias	description
SendMail <letter ID> [true]	x	
Obsolete since Stardew Valley 1.6. Use a trigger action in the event instead to send mail.

For the current player: mark this event as seen, add the specified letter to tomorrow's mail, then abort without starting the event. Use the optional argument true to add the letter directly to the mailbox instead of sending it tomorrow.

In addition, some older preconditions have been replaced by the newer ! syntax:

name	alias	newer equivalent
NotActiveDialogueEvent	A	!ActiveDialogueEvent
NotCommunityCenterOrWarehouseDone	X	!CommunityCenterOrWarehouseDone
NotDayOfWeek	d	!DayOfWeek
NotFestivalDay	F	!FestivalDay
NotHostMail	Hl	!HostMail
NotHostOrLocalMail	*l	!HostOrLocalMail
NotLocalMail	l	!LocalMail
NotRoommate	Rf	!Roommate
NotSawEvent	k	!SawEvent
NotSeason	z	!Season
NotSpouse	o	!Spouse
NotUpcomingFestival	U	!UpcomingFestival
Event scripts

Basic format

Each event has a value which is the event script. This specifies what happens in the event — everything from lighting and music to NPC movement and dialogue. The script consists of multiple commands separated by forward slash (/) characters. Event commands are quote-aware, so you can use spaces and slashes in arguments like speak Penny \"I'm running A/B tests\" without needing to escape them (the quote marks themselves do need to be escaped).

Every script must start with three commands in this exact order:

index	syntax	description
0	<music ID>	The background music or ambient sounds to play during the event. This can be...
an audio track ID to play;
or none to stop any existing music and use the default ambient background noise for the location;
or continue to keep playing the current background music.
This can be changed later using the playMusic or stopMusic event commands.

1	<x> <y>	The tile coordinates the camera should center on at the start of the event.
2	[<character ID> <x> <y> <direction>]+	Initialises one or more characters' starting tile positions and directions. The character ID can be farmer or an NPC name like Abigail. Note: Unlike other commands with a <direction> argument, directions in this command must be numeric values.
Those three commands may be followed by any sequence of the following commands:

command	description
action <action>	Run a trigger action string, like action AddMoney 500 to add Gold.png500g to the current player.
addBigProp <x> <y> <object ID>	Adds an object at the specified tile from the TileSheets\Craftables.png sprite sheet.
addConversationTopic <ID> [length]	Starts a conversation topic with the given ID and day length (or 4 days if no length given). Setting length as 0 will have the topic last only for the current day.
addCookingRecipe <recipe>	Adds the specified cooking recipe to the player.
addCraftingRecipe <recipe>	Adds the specified crafting recipe to the player.
addFloorProp <prop index> <x> <y> [solid width] [solid height] [display height]	Add a non-solid prop from the current festival texture. Default solid width/height is 1. Default display height is solid height.
addItem <item ID> [count] [quality]	Add an item to the player inventory (or open a grab menu if their inventory is full). The <item ID> is the qualified or unqualified item ID, and [quality] is a numeric quality value.
addLantern <row in texture> <x> <y> <light radius>	Adds a glowing temporary sprite at the specified tile from the Maps\springobjects.png sprite sheet. A light radius of 0 just places the sprite.
addObject <x> <y> <item ID> [layer]	Adds a temporary sprite at the tile specified by <x> and <y> using a qualified or unqualified item ID from Data/Objects. Layer (default -1) refers to the depth at which the sprite will be drawn on the screen, with higher numbers meaning it will be drawn on top of other things. Can be negative numbers. Any number above 0 will draw the object on top of NPCs.
addProp <prop index> <x> <y> [solid width] [solid height] [display height]	Add a solid prop from the current festival texture. Default solid width/height is 1. Default display height is solid height.
addQuest <quest ID>	Add the specified quest to the quest log.
addSpecialOrder <order ID>	Add a special order to the player team. This affects all players, since special orders are shared.
addTemporaryActor <spriteAssetName> <sprite width> <sprite height> <tile x> <tile y> <direction> [breather] [Character|Animal|Monster] [override name]	Add a temporary actor. spriteAssetName is the name of the sprite to add (e.g., Ghost). Underscores in the asset name are now only replaced with spaces if an exact match wasn't found. You should quote arguments containing spaces instead, like addTemporaryActor "White Chicken" ….
[breather] is boolean (default true).

[Character|Animal|Monster] (default Character) determines whether the sprite will be loaded from "Characters/", "Animals/", or "Characters/Monsters/".

[override name] can be used to give the temporary actor a different name.

advancedMove <actor> <loop> <x y>... OR <direction duration>	Set multiple movements for an actor. You can set True to have the actor walk the path continuously. Example: /advancedMove Robin false 0 3 2 0 0 2 -2 0 0 -2 2 0/
To make the actor move along the x axis (left/right), use the number of tiles to move and 0. For example, -3 0 will cause the actor to walk three tiles to the left while facing left. 2 0 will cause the actor to walk two tiles to the right while facing right.

To make the actor move along the y axis (up/down), use 0 and the number of tiles to move. For example, 0 1 will cause the actor to walk one tile down while facing down. 0 -5 will cause the actor to walk five tiles up while facing up.

To make an actor pause, use the direction to face and the number of milliseconds to pause. 1 is right, 2 is down, 3 is left, and 4 is up. The reason 4 is up and not 0 is so that advancedMove can tell the difference between a pause command and a move up/down command.

The code can tell the difference between a move command and a pause command because a move command must have 0 for either x or y. A pause command must have non-zero numbers for both numbers in the pair.

Example: /advancedMove Clint true 4 0 2 5000 -4 0 1 3000/ Clint will have continuous movement moving 4 tiles to the right, facing down upon arriving, waiting for 5 seconds, then moving 4 tiles to the left, facing right upon arriving, then waiting for 3 seconds, then loops because the loop was set to true(see above).

Example: /advancedMove Pam true 5 0 0 3 3 5000 -6 0 0 -4/ Pam first moves 5 tiles to the right, then directly moves 3 tiles downward, faces the to the left upon arriving then waits 5 seconds before moving 6 tiles to the left then moves up 4 tiles directly.

ambientLight <r> <g> <b>	Modifies the ambient light level, with RGB values from 0 to 255.
animalNaming	Show the animal naming menu if no other menu is open. Uses the current location as Coop. Appears to only work for 'hatched' animals.
animate <actor> <flip> <loop> <frame duration> <frames...>	Animate a named actor, using one or more <frames> from their sprite sheet, for <frame duration> milliseconds per frame. <frames> are indexed numerically, based on 16x32 pieces of the image. This index increases as you go from left to right, starting from 0. <flip> indicates whether to flip the sprites along the Y axis; <loop> indicates whether to repeat the animation until stopAnimation is used. If you're animating the farmer, it may be helpful to reference Modding:Farmer_sprite#Sprite_Index_Breakdown
attachCharacterToTempSprite <actor>	Attach an actor to the most recent temporary sprite.
awardFestivalPrize	Awards the festival prize to the winner for the easter egg hunt and ice fishing contest. Will not do anything outside those festivals; see the awardFestivalPrize entry with arguments just below for how to use it in events.
awardFestivalPrize <item type|item id>	Awards the specified item to the player (shows a HUD message and adds the item to the player's inventory). Possible item types are "pan", "sculpture", "rod", "sword", "hero", "joja", "slimeegg", "emilyClothes", and "jukebox". item id can be any qualified item id. This command causes an event to skip the next command that comes after it, so it's recommended to add a pause command to stop it skipping anything important.
beginSimultaneousCommand	Starts a block of commands which the game will attempt to execute at the same time. End the block with its partner endSimultaneousCommand, like so:
beginSimultaneousCommand/<command>/<command>/.../endSimultaneousCommand

If the commands can be completed instantly, they will all be set to run on the same tick. Normally, each command in an event script starts a minimum of one tick after the previous command, so this block lets you compress a sequence of quick things down to one tick (some examples of "quick" commands: showFrame, positionOffset, warp, etc.). However, any event commands which block the event loop and consume time will still do so even inside a simultaneousCommand block. This applies to move (without its optional true argument to remove the blocking behavior), faceDirection (ditto), emote (ditto), speak, pause, and many others. Commands like this are not suitable for use in these blocks, and commands following them will not execute simultaneously.

As a result of the blocking command behavior, this command is not suitable for achieving parallel action in an event (e.g. one character walks to a target spot while another speaks); for that you will want to use optional trues, advancedMove, and the like. This command is only useful if you want to avoid a one-frame delay between instant commands, for example between addItem and setSkipActions (one-frame window to get an item twice), or between showFrame and positionOffset (one frame where only one is applied).

broadcastEvent [bool useLocalFarmer]	Makes the event a "broadcast event", forcing others players connected to also see it. Any players connected will be forced to warp to this event. This is used in some vanilla events like when Lewis shows the farmer the abandoned Community Center.
If true, useLocalFarmer will make the Farmer actor in the event the individual player's Farmer, otherwise if omitted or false it will be the host's Farmer. The useLocalFarmer option isn't used anywhere in the vanilla game.

catQuestion	Trigger question about adopting your pet.
cave	Trigger the question for the farm cave type. This will work again later, however changing from bats to mushrooms will not remove the mushroom spawning objects.
changeLocation <location>	Change to another location and run the remaining event script there.
changeMapTile <layer> <x> <y> <tile index>	Change the specified tile to a particular value.
changeName <actor> <displayName>	Sets the display name of the actor to displayName. Quote arguments containing spaces, like changeName Leo "Neo Leo".
changePortrait <npc> [portrait]	Change the portrait asset for specified NPC's portrait to one matching the form "Portraits/<actor>_[portrait]". For example, changePortrait Shane JojaMart would make Shane use his JojaMart portrait. Repeat the command without the [portrait] argument to change the NPC back to their normal portrait.
changeSprite <actor> [sprite]	Change the sprite asset for specified NPC's portrait to one matching the form "Characters/<actor>_[sprite]". For example, changeSprite Shane JojaMart would make Shane use his JojaMart sprite. Repeat the command without the [sprite] argument to change the NPC back to their normal sprite.
changeToTemporaryMap <map> [pan]	Change the location to a temporary one loaded from the map file specified by <map>. The [pan] argument is broken — the screen will not pan regardless of what value it is given.
changeYSourceRectOffset <npc> <offset>	Changes the NPC's vertical texture offset. Example: changeYSourceRectOffset Abigail 96 will offset her sprite sheet, showing her looking left instead of down. This persists for the rest of the event. This is only used in Emily's Clothing Therapy event to display the various outfits properly.
characterSelect	Seemingly unused. Sets Game1.gameMode to 5 and Game1.menuChoice = 0.
cutscene <cutscene>	Activate a cutscene. See cutscene list.
doAction <x> <y>	Acts as if the player had clicked the specified x/y coordinate and triggers any relevant action. It is commonly used to open doors from inside events, but it can be used for other purposes. If you use it on an NPC you will talk to them, and if the player is holding an item they will give that item as a gift. doAction activates objects in the main game world (their actual location outside of the event), so activating NPCs like this is very tricky, and their reaction varies depending on what the player is holding.
dump <group>	Starts the special "cold shoulder" and "second chance" dialogue events for the given group (women if group is girls and men if it is anything else.) The cold shoulder event has an id of dumped_Girls or dumped_Guys and lasts 7 days; the second chance event has an id of secondChance_Girls or secondChance_Guys and lasts 14 days. During open beta testing of version 1.3 there was a second parameter which determined the amount of hearts lost, but support for that parameter was removed before release.
elliotbooktalk	Elliot book talk.
emote <actor> <emote ID> [continue]	Make the given NPC name perform an emote, which is a little icon shown above the NPC's head. If continue is specified, the next command will play out immediately. Emotes are stored in Content\TileSheets\emotes.xnb; see list of emotes.
end	Ends the current event by fading out, then resumes the game world and places the player on the square where they entered the zone. All end parameters do this by default unless otherwise stated.
end bed	Same as end, but warps the player to the x/y coordinate of their most recent bed. This does not warp them to the farmhouse, only to the x/y coordinate of the bed regardless of map.
end beginGame	Used only during the introduction sequence in the bus stop event. It sets the game mode to playingGameMode, warps the player to the farmhouse (9, 9), ends the current event, and starts a new day.
end credits	Not used in any normal events. Clears debris weather, changes the music to wedding music, sets game mode to creditsMode and ends the current event.
end dialogue <NPC> <"Text for next chat">	Same as end, and additionally clears the existing NPC dialogue for the day and replaces it with the line(s) specified at the end of the command. Example usage: end dialogue Abigail "It was fun talking to you today.$h"
end dialogueWarpOut <NPC> <"Text for next chat">	See end dialogue and end warpOut.
end invisible <NPC>	Same as end, and additionally turns the specified NPC invisible (cannot be interacted with until the next day).
end invisibleWarpOut <NPC>	See end invisible and end warpOut.
end newDay	Ends both the event and the day (warping player to their bed, saving the game, selling everything in the shipping box, etc).
end position <x> <y>	Same as end, and additionally warps the player to the map coordinates specified in x y.
end warpOut	Same as end, and additionally finds the first warp out of the current location (second warp if male and in the bathhouse), and warps the player to its endpoint.
end wedding	Used only in the hardcoded wedding event. Changes the character's clothes back to normal, sets Lewis' post-event chat to "That was a beautiful ceremony. Congratulations!$h", and warps the player to their farm.
endSimultaneousCommand	Ends a simultaneous command block started by beginSimultaneousCommand.
eventSeen <event ID> [seen]	Add or remove an event ID from the player's list of seen events, based on [seen] (default true to add).
Can be used to prevent a mutually exclusive event from being seen by setting it to true.

An event can mark itself unseen using eventSeen <event ID> false. This takes effect immediately (i.e. the event won't be added to the list when it completes), but the game will prevent the event from playing again until the player changes location to avoid event loops. The event will still replay when re-entering the location, making it useful for creating repeatable events.

extendSourceRect <actor> reset	Resets the actors sprite.
extendSourceRect <actor> <horizontal> <vertical> [ignoreUpdates]
extendSourceRect
eyes <eyes> <blink>	Change the player's eyes. Eyes is represented by and Integer from 0 - 5 (open, closed, right, left, half closed, wide open). Blink is a timer that is represented with a negative number. -1000 is the default timer.
faceDirection <actor> <direction> [continue]	Make a named NPC face a direction. If [continue] is false (default) the game will pause while the NPC changes their direction.
fade [unfade]	Fades out to black if no parameter is supplied. If the parameter is unfade (not true), fades in from black.
farmerAnimation <anim>	Briefly sets the farmer's sprite to <anim> for a variable (depending on sprite) interval. Only used once in vanilla events. Using showFrame farmer <sprite> twice (to set a new frame and back) is more powerful as it lets you control the interval using pause n.
farmerEat <object ID>	Make the player eat an object. (The farmer actually does eat the object, so buffs will apply, healing will occur, etc.)
fork [req] <event ID>	End the current command script and starts a different script with the given ID, but only if the [req] condition is met. (Example: /fork choseWizard finalBossWizard in the "Necromancer" script of Sebastian's 6-heart event.) The [req] condition can be a mail ID or dialogue answer ID; if not specified, it checks if the specialEventVariable1 variable was set (e.g., by a question event command or %fork dialogue command). The new script should have the same format as a normal event script, but without the mandatory three start fields.
friendship <npc> <amount>	Add the given number of friendship points with the named NPC. (There are 250 points per heart.)
globalFade [speed] [continue]	Fade to black at a particular speed (default 0.007). If [continue] is false (default) the event pauses during the fade, otherwise the event continues as the screen fades to black. The fade effect disappears when this command is done; to avoid that, use the viewport command to move the camera off-screen.
globalFadeToClear [speed] [continue]	Fade in from black at a particular speed (default 0.007). If [continue] is false (default) the event pauses during the fade, otherwise the event continues as the screen fades in. If the screen is not already black when this command starts it will abruptly set it to black before fading back in.
glow <r> <g> <b> <hold>	Make the screen glow once, fading into and out of the <r> <g> <b> values over the course of a second. If <hold> is true it will fade to and hold that color until stopGlowing is used.
grandpaCandles	Do grandpa candles
grandpaEvaluation	Do grandpa evaluation
grandpaEvaluation2	Do grandpa evaluation (manually resummoned)
halt	Make everyone stop.
hideShadow <actor> <true/false>	Hide the shadow of the named actor. False unhides it.
hospitaldeath	Forces you to lose money and items, pulls up the dialogue box with Harvey. IS NOT THE SAME AS ANY OF THE END COMMANDS.
ignoreCollisions <character ID>	Make a character ignore collisions when moving for the remainder of the event. For example, they'll walk through walls if needed to reach their destination. The character ID can be farmer or an NPC name like Abigail.
ignoreEventTileOffset	Tile positions in farm events are offset to match the farmhouse position. If an event shouldn't be based on the farmhouse position, add an ignoreEventTileOffset command to disable the offset for that event. This must be the 4th command (after the 3 initial setup ones) to take effect.
ignoreMovementAnimation <actor> [ignore]	Causes the specified NPC to move without their walk animations, based on [ignore] (default true to ignore animation).
itemAboveHead [type|item id]	Show an item above the player's head and displays a message about it. The [type] can be "pan", "hero", "sculpture", "joja", "slimeEgg", "rod", "sword", or "ore". [item id] can be any qualified item id. If no item is specified, then they will hold the furnace blueprint and no message will be displayed. item id can be any qualified item id. This command does not add the item to the player's inventory (except when used with "slimeEgg").
The message box from itemAboveHead will cancel dialogue from commands that come after it unless at least 1,200ms has passed, so it is recommended to use a pause command after itemAboveHead.

jump <actor> [intensity]	Make a the named NPC jump. The default intensity is 8.
loadActors <layer>	Load the actors from a layer in the map file.
makeInvisible <x> <y> [x-dimension] [y-dimension]	Temporarily hides selected objects or terrain features: the tile(s) will become passable for the duration of the event. Useful for clearing a walking area during events, especially in the FarmHouse. (Example: /makeInvisible 8 14 hides any object or terrain feature at tile 8, 14 in the current map.) The optional [x-dimension] and [y-dimension] arguments allow you to specify a larger area to be cleared. (Example: /makeInvisible 68 36 13 7 in Leah's 14-heart event clears a 13 × 7 tile rectangular area with the top-left corner at coordinate 68, 36.). Known bugs: some furniture may not re-appear immediately?
mail <letter ID>	Queue a letter to be received tomorrow (see Content\Data\mail.xnb for available mail).
mailReceived <letter ID> [add]	Add or remove a letter to the player's received list (bypassing the mailbox), based on [add] (default true to add). Useful for setting mail flags.
Its old name is addMailReceived which can still be used as an alias.

mailToday <letter ID>	Adds a letter to the mailbox immediately, given the <letter ID> in Data/Mail.
message "<text>"	Show a dialogue box (no speaker). See dialogue format for the <text> format.
minedeath	Forces you to lose money and items, pulls up the dialogue box with Harvey. IS NOT THE SAME AS ANY OF THE END COMMANDS.
money <amount>	Adds/removes the specified amount of money.
move <actor> <x> <y> <direction> [continue]	Make a named NPC move by the given tile offset from their current position (along one axis only), and face the given direction when they're done. To move along multiple axes, you must specify multiple move commands. By default the event pauses while a move command is occurring, but if [continue] (default false) is set to true the movement is asynchronous and will run simultaneously with other event commands. You can also move multiple people at a time in a single event command with move - an example of /move Abigail 1 0 1 Sam 0 1 1 Sebastian 2 0 1/ will have Abigail, Sam, and Sebastian all move at the same time in their respective directions.
pause <duration>	Pause the game for the given number of milliseconds.
playMusic <track>	Play the specified music track ID. If the track is 'samBand', the track played will change depend on certain dialogue answers (76-79).
playSound <sound>	Play a given sound ID from the game's sound bank.
playerControl	Give the player control back.
positionOffset <actor> <x> <y> [continue]	Offset the position of the named NPC by the given number of pixels. This happens instantly, with no walking animation. If [continue] is false (default) the event will pause while the NPC's position is being offset.
proceedPosition <actor>	Waits for the specified actor to stop moving before executing the next command.
question null "<question>#<answer1>#<answer2>"	Show a dialogue box with some answers and an optional question. When the player chooses an answer, the event script continues with no other effect.
question fork<answer index> "<question>#<answer 0>#<answer 1>#..."	Show a dialogue with some answers and an optional question. When the player chooses the answer matching the fork<answer index> (like fork0 for the first answer), the specialEventVariable1 variable is set. Usually followed by a fork command. Example:
.../question fork0 \"#answer0#answer1#answer3\"/fork eventidhere/..."
questionAnswered <answer ID> [answered]	Add or remove an answer ID from the player's list of chosen dialogue answers, based on [answered] (default true to add).
quickQuestion <question>#<answer1>#<answer2>#<answer3>(break)<answer1 script>(break)<answer2 script>(break)<answer3 script>	Show a dialogue box with an optional question and some answers. The answer scripts are sequences of commands separated by \\. When the player chooses an answer, the relevant answer script is executed and then the event continues. Usually used when an NPC's response will depend on the answer chosen but nothing else in the event has to depend on it.
Note: If quickQuestion is used immediately at the start of an event block (Example: "ExampleEvent": "quickQuestion [rest of the event]"), it will cause a dialogue loop. Adding another command in front of quickQuestion resolves this issue (Example: "ExampleEvent": "pause 1/quickQuestion [rest of the event]").
removeItem <object ID> [count]	Remove one instance of an item from a player's inventory. Use [count] to specify the number to be removed.
removeObject <x> <y>	Remove the prop at a position.
removeQuest <quest ID>	Remove the specified quest from the quest log.
removeSpecialOrder <order ID>	Remove a special order from the player team. This affects all players, since special orders are shared.
removeSprite <x> <y>	Remove the temporary sprite at a position.
removeTemporarySprites	Remove all temporary sprites.
removeTile <x> <y> <layer>	Remove a tile from the specified layer.
replaceWithClone <NPC name>	Replace an NPC already in the event with a temporary copy. This allows changing the NPC for the event without affecting the real NPC.
For example, this event changes Marnie's name/portrait only within the event:

"Some.ModId_ExampleEvent/": "continue/64 15/farmer 64 15 2 Marnie 64 17 0/replaceWithClone Marnie/changeName Marnie Pufferchick/changePortrait Marnie Pufferchick/[...]/end"
resetVariable	Set the specialEventVariable1 to false.
rustyKey	Gives the player the rusty key. (Sewer key)
screenFlash <alpha>	Flashes the screen white for an instant. An alpha value from 0 to 1 adjusts the brightness, and values from 1 and out flashes pure white for x seconds.
setRunning	Set the player as running.
setSkipActions [actions]	Set trigger actions that should run if the player skips the event. You can list multiple actions delimited with #, or omit [actions] so no actions are run. When the player skips, the last setSkipActions before that point is applied.
For example, this adds the garden pot recipe and item to the player if the event is skipped, but avoids adding the item if the event already did it:

/setSkipActions AddCraftingRecipe Current "Garden Pot"#AddItem (BC)62
/skippable
/...
/addItem (BC)62
/setSkipActions AddCraftingRecipe Current "Garden Pot"
Skip actions aren't applied if the event completes normally without being skipped.

shake <actor> <duration>	Shake the named NPC for the given number of milliseconds.
showFrame farmer <frame> <flip>	Flip the farmer's current sprite along the Y axis. Farmer looks strange if not facing the correct direction prior to 1.6. Flip is a true/false value.
showFrame <actor> <frame ID>	Set the named NPC's current frame in their Content\Characters\*.xnb spritesheet. Note that setting the farmer's sprite only changes parts of the sprite (some times arms, some times arms and legs and torso but not the head, etc). To rotate the whole sprite, use faceDirection farmer <0/1/2/3> first before modifying the sprite with showFrame. Frame ID starts from 0. If farmer is the one whose frame is being set, "farmer" can be eliminated, i.e., both showFrame farmer <frame ID> and showFrame <frame ID> would work.
skippable	Allow skipping this event.
speak <character> "<text>"	Show dialogue text from a named NPC; see dialogue format. The quote marks need to be escaped (\") when events are written in JSON as with Content Patcher. For example speak Leah \" My dialogue has been escaped! \".
specificTemporarySprite <sprite> [other params]	Shows the given temporary sprite. Parameters change depending on the sprite. These are quite hardcoded and probably not easily reusable, other than the little heart.
speed farmer <modifier>	Add a speed modifier to the farmer. Is persistent and you will have to use the command again to return to normal speed.
speed <actor> <speed>	Sets the named NPC's speed (default speed is 3). Not applicable to the farmer. Applies only through the end of the next movement or animation on that NPC.
splitSpeak <actor> "<text>"	Dialogue, but chosen based on previous answer. ('~' is the separator used.)
startJittering	Make the player start jittering.
stopAdvancedMoves	Stop movement from advancedMove.
stopAnimation farmer	Stop the farmer's current animation.
stopAnimation <actor> [end frame]	Stop the named NPC's current animation. Not applicable to the farmer.
stopGlowing	Make the screen stop glowing.
stopJittering	Make the player stop jittering.
stopMusic	Stop any currently playing music.
stopRunning	Make the farmer stop running.
stopSound <sound ID> [immediate]	Stop a sound started with the startSound command. This has no effect if the sound has already stopped playing on its own (or hasn't been started). If you started multiple sounds with the same ID, this will stop all of them (e.g. startSound fuse/startSound fuse/stopSound fuse).
By default the sound will stop immediately. For looping sounds, you can pass false to the [immediate] argument to stop the sound when it finishes playing the current iteration instead (e.g. stopSound fuse false).

stopSwimming <actor>	Make an actor stop swimming.
swimming <actor>	Make an actor start swimming.
switchEvent <event ID>	Changes the current event (ie. event commands) to another event in the same location.
temporarySprite <x> <y> <row in texture> <animation length> <animation interval> <flipped> <layer depth>	Create a temporary sprite with the given parameters, from the resource TileSheets/animations.
temporaryAnimatedSprite ...	Add a temporary animated sprite to the event, using these space-delimited fields:
index	field	effect
0	texture	The asset name for texture to draw.
1–4	rectangle	The pixel area within the texture to draw, in the form <x> <y> <width> <height>.
5	interval	The millisecond duration for each frame in the animation.
6	frames	The number of frames in the animation.
7	loops	The number of times to repeat the animation.
8–9	tile	The tile position at which to draw the sprite, in the form <x> <y>.
10	flicker	Causes the sprite to flicker in and out of view repeatedly. (one of true or false).
11	flip	Whether to flip the sprite horizontally when it's drawn (one of true or false).
12	sort tile Y	The tile Y position to use in the layer depth calculation, which affects which sprite is drawn on top if two sprites overlap. The larger the number, the higher layer the sprite is on.
13	alpha fade	Fades out the sprite based on alpha set. The larger the number, the faster the fade out. 1 is instant.
14	scale	A multiplier applied to the sprite size (in addition to the normal 4× pixel zoom).
15	scale change	Changes the scale based on the multiplier applied on top of the normal zoom. Continues endlessly.
16	rotation	The rotation to apply to the sprite, measured in radians.
17	rotation change	Continuously rotates the sprite, causing it to spin. The speed is determined by input value.
18+	flags	Any combination of these space-delimited flags:
color <color>: apply a standard color to the sprite.
hold_last_frame: after playing the animation once, freeze the last frame as long as the sprite is shown.
ping_pong: Causes the animation frames to play forwards and backwards alternatingly. Example: An animation with the frames 0, 1, 2 will play "0 1 2 0 1 2" without the ping_pong flag and play "0 1 2 1 0" with the ping_pong flag.
motion <x> <y>: Sprite moves based on the values set. Numbers can be decimals or negative.
acceleration <x> <y>: [TODO: document what this does]
acceleration_change <x> <y>: [TODO: document what this does]
textAboveHead <actor> \"<text>\"	Show a small text bubble over the named NPC's head with the given text; see dialogue format. Note: @ does not get replaced with the farmer's name the way it does in dialogue or with the speak command. Instead, use the Content Patcher token {{PlayerName}}.
tossConcession <actor> <concessionId>	Causes an NPC to throw their concession in the air. concessionId is from Data/Concessions.
translateName <actor> <translation key>	Set the display name for an NPC in the event to match the given translation key.
tutorialMenu	Show the tutorial menu if no other menu is open.
updateMinigame <event data>	Send an event to the current minigame.
viewport move <x> <y> <duration>	Pan the the camera in the direction (and with the velocity) defined by x/y for the given duration in milliseconds. Example: "viewport move 2 -1 5000" moves the camera 2 pixels right and 1 pixel up for 5 seconds. The total distance moved is based on framerate, rather than time, and may be inconsistent.
viewport <x> <y> [true [unfreeze]|clamp [true|unfreeze]]	Instantly reposition the camera to center on the given X, Y tile position. TODO: explain other parameters.
waitForAllStationary	Waits for all actors to stop moving before executing the next command.
waitForOtherPlayers	Wait for other players (vanilla MP).
warp <actor> <x> <y> [continue]	Warp the named NPC to a position to the given X, Y tile coordinate. This can be used to warp characters off-screen. If [continue] is false (default) the event will pause while the NPC is being warped.
warpFarmers [<x> <y> <direction>]+ <default offset> <default x> <default y> <direction>	Warps connected players to the given tile coordinates and numeric directions. The <x> <y> <direction> triplet is repeated for each connected player (e.g. the first triplet is for the main player, second triplet is for the first farmhand, etc). The triplets are followed by an offset direction (one of up, down, left, or right), and a final triplet which defines the default values used by any other player.
Some commands are broken or unusable:

command	description
end busIntro	Supposed to start the bus intro scene, presumably the one that was cut before release.
A command which begins with -- is ignored. This can be used to insert comments or to disable commands temporarily while debugging an event. The comment ends at the next slash delimiter (/), so it can't contain slashes. Comments cannot be added between the first three commands (music, viewport coordinates, character setup). This means you can't add a comment after the music or viewport coordinates commands, but you can add a comment after character setup.

For example:

"666/": "none/-1000 -1000/farmer 5 7 0/--you can add comments from here/skippable/viewport 5 7 10/--set viewport near door/pause 2000/end"
Multilining

Event commands trim surrounding whitespace, so they can be multilined for readability. Line breaks must be before or after the / delimiter. For example:

"SomeEventId/": "
    none/
    -1000 -1000/
    farmer 5 7 0/
    ...
"
This can be also combined with the comment syntax to add notes. (Reminder: comments can't be used between the first three commands). For example:

"SomeEventId/": "
    none/            
    -1000 -1000/     
    farmer 5 7 0/    -- actor positions/
    ...
"
Optional NPCs

You can mark NPCs optional in event commands by suffixing their name with ?. For example, jump Kent? won't log an error if Kent is missing. When used in the initial event positions, the NPC is only added if they exist in Data/Characters and their UnlockConditions match.

Directions

When using an event command with a direction argument, use one of these numeric values or case-insensitive names:

Numeric Value	Name	Meaning
0	up	looking up
1	right	looking right
2	down	looking down
3	left	looking left
Cutscenes

The cutscene command will accept the following values:

Value	In-game scene
AbigailGame	Sets up Journey of the Prairie King mini-game with Abigail
addSecretSantaItem	Feast of the Winter Star gift scene
balloonChangeMap	Changes to the "in the balloon" map for Harvey's 10 heart event
balloonDepart	Balloon taking off in Harvey's 10 heart event
bandFork	Plays correct scene for Sam's 8 heart event, based on response given in 2 heart event
boardGame	Sets up Solarion Chronicles mini-game
clearTempSprites	Removes all temporary sprites from map
eggHuntWinner	Egg Hunt winner scene
governorTaste	Luau soup scene
greenTea	Caroline sunroom scene from her 2 heart event
haleyCows	Haley meeting the cows from her 8 heart event
iceFishingWinner	Festival of Ice fishing contest winner scene
iceFishingWinnerMP	Sets up text message displayed when leaving Festival of Ice
linusMoneyGone	??? (seems to be unused)
marucomet	Maru Comet scene from her 14 heart event
plane	Plane flying by from Harvey's 8 heart scene
robot	MarILDA taking off from Maru's 10 heart scene
Dialogue format

See Modding:Dialogue#Format.

Common values

Emotes

An emote is an animated icon bubble shown above an NPC's head to indicate a mood or reaction. Emote icons are stored in the TileSheets\emotes asset.

ID	Character constant
4	emptyCanEmote
8	questionMarkEmote
12	angryEmote
16	exclamationEmote
20	heartEmote
24	sleepEmote
28	sadEmote
32	happyEmote
36	xEmote
40	pauseEmote
44	unused
48	unused
52	videoGameEmote
56	musicNoteEmote
60	blushEmote
Using C#

You can add custom event preconditions & commands using new Event methods.

Method
RegisterPrecondition
RegisterPreconditionAlias
RegisterCommand
RegisterCommandAlias
These are some useful fields:

Field	Usage
Event.fromAssetName	Indicates which data asset (if any) the event was loaded from.
Game1.eventsSeenSinceLastLocationChange	Tracks the events that played since the player arrived in their current location.


Data

Overview

You can change the world map by editing the Data/WorldMap asset. You can add custom maps for certain locations, apply texture overlays, add/edit tooltips, set player marker positioning, etc.

The game divides the world map into three main concepts (see example at right):

A region is a large-scale part of the world containing everything shown on the map. For example, the default world map is the Valley region.
A map area is a subset of the world map which optionally add tooltips, scroll text, texture overlays, and player marker positioning info.
A map area position matches in-game locations and tile coordinates to the drawn world map. The game uses this to automatically position player markers at a relative position on the world map (e.g. so you can watch other players move across the location on the map).
In the data model:

each entry is a region;
each entry's MapAreas are the region's map area;
and each map area's WorldPositions are the world map positions.
The game will find the first WorldPositions entry which matches the current location, and assume you're in the region and map area which contains it. If there's none found, it defaults to the farm.

Format

The Data/WorldMap data asset consists of a string → model lookup, where...

The key is a unique string ID for the region.
The value is a model with the fields listed below.
field	effect
BaseTexture	(Optional) The base texture to draw for the map, if any. The first matching texture is applied. If map areas provide their own texture too, they're drawn on top of this base texture.
This consists of a list of models with these fields:

field	effect
Id	The unique string ID for the texture entry within the list.
Texture	The asset name for the texture to draw.
SourceRect	(Optional) The pixel area within the Texture to draw, specified as an object with X, Y, Width, and Height fields. Defaults to the entire texture image.
MapPixelArea	(Optional) The pixel area within the map which is covered by this area, specified as an object with X, Y, Width, and Height fields. If omitted, draws the entire SourceRect area starting from the top-left corner of the map.
Condition	(Optional) A game state query which indicates whether this texture should be selected. Defaults to always selected.
MapAreas	The areas to draw on top of the BaseTexture. These can provide tooltips, scroll text, texture overlays, and player marker positioning info.
This consists of a list of models with these fields:

field	effect
Id	The unique string ID for the map area within the list.
PixelArea	The pixel area within the map which is covered by this area. This is used to set the default player marker position, and is the default value for pixel areas in other fields below.
ScrollText	(Optional) A tokenizable string for the scroll text (shown at the bottom of the map when the player is in this area). Defaults to none.
Textures	(Optional) The image overlays to apply to the map. All matching textures are applied.
This consists of a list of models with these fields:

field	effect
Id	The unique string ID for the texture entry within the area.
Texture	The asset name for the texture to draw.
If set to the exact string MOD_FARM, the game will apply the texture for the current farm type (regardless of whether it's a vanilla or mod farm type). This should usually be used with "MapPixelArea": "0 43 131 61" (the farm area on the default map).

SourceRect	(Optional) The pixel area within the Texture to draw, specified as an object with X, Y, Width, and Height fields. Defaults to the entire texture image.
MapPixelArea	(Optional) The pixel area within the map which is covered by this area, specified as an object with X, Y, Width, and Height fields. If omitted, defaults to the map area's PixelArea.
Condition	(Optional) A game state query which indicates whether this texture should be selected. Defaults to always selected.
Tooltips	(Optional) The tooltips to show when hovering over parts of this area on the world map.
This consists of a list of models with these fields:

field	effect
Id	The unique string ID for the tooltip within the area.
Text	(Optional) A tokenizable string for the text to show in a tooltip.
PixelArea	(Optional) The pixel area within the map which can be hovered to show this tooltip. Defaults to the area's PixelArea.
Condition	(Optional) A game state query which indicates whether this tooltip should be available. Defaults to always available.
KnownCondition	(Optional) A game state query which indicates whether the area is known by the player, so the Text is shown as-is. If this is false, the tooltip text is replaced with ???. Defaults to always known.
LeftNeighbor
RightNeighbor
UpNeighbor
DownNeighbor	(Optional) When navigating the world map with a controller, the tooltip to snap to when the player moves the cursor while it's on this tooltip.
This must specify the area and tooltip formatted like areaId/tooltipId (not case-sensitive). If there are multiple possible neighbors, they can be specified in comma-delimited form; the first valid one will be used.

For example, this will snap to the community center when the user moves the cursor to the right:

"RightNeighbor": "Town/CommunityCenter"
A blank value will be ignored, but the game will log a warning if you specify neighbor IDs and none of them match. To silently ignore them instead (e.g. for a conditional location), you can add 'ignore' as an option:

"RightNeighbor": "Town/SomeOptionalLocation, ignore"
See also the MapNeighborIdAliases field in the region data.

WorldPositions	(Optional) The in-world locations and tile coordinates to match to this map area. The game uses this to automatically position player markers at a relative position on the world map (e.g. so you can watch other players move across the location on the map).
This consists of a list of models with these fields:

field	effect
Id	The unique string ID for this entry within the area.
LocationContext	(Optional) The location context in which this world position applies. The vanilla contexts are Default (valley) and Island (Ginger Island).
LocationName	(Optional) The location name to which this world position applies. Any location within the mines and the Skull Cavern will be Mines and SkullCave respectively, and festivals use the map asset name (like Town-EggFestival).
LocationNames	(Optional) Equivalent to LocationName, but you can specify multiple locations as an array.
TileArea
MapPixelArea	(Optional) The tile area within the in-game location (TileArea) and the equivalent pixel area on the world map (MapPixelArea). These are used to calculate the position of a character or player within the map view, given their real position in-game. For example, if the player is in the top-right corner of the tile area in-game, they'll be shown in the top-right corner of the drawn area on the world map.
Both are specified as an object with X, Y, Width, and Height fields. TileArea defaults to the entire location, and MapPixelArea defaults to the map area's PixelArea.

ScrollText	(Optional) A tokenizable string for the scroll text shown at the bottom of the map when the player is within this position. Defaults to the map area's ScrollText, if any.
Condition	(Optional) A game state query which indicates whether this entry should be applied. Defaults to always applied.
ScrollTextZones	(Optional, specialized) Smaller areas within the world map position which have their own scroll text (like "Mountains" vs "Mountain Lake" in the mountain area).
This consists of a list of models with these fields:

field	effect
Id	The unique string ID for this entry within the list.
TileArea	The tile area within the position's TileArea for which this entry applies. See details on the parent field.
ScrollText	A tokenizable string for the scroll text shown at the bottom of the map when the player is within this scroll text zone.
ExtendedTileArea	(Optional, specialized) The tile area within the in-game location to which this position applies, including tiles that are outside the TileArea. The ExtendedTileArea must fully contain TileArea, since the latter won't be checked for the initial detection anymore.
For example, let's say we have this ExtendedTileArea (the larger box) and TileArea (the smaller box inside it), and the player is at position X:

┌────────────────────┐
│    ┌────────┐      │
│ X  │        │      │
│    │        │      │
│    └────────┘      │
└────────────────────┘
In this case, the entry would be selected (since the player is inside the ExtendedTileArea), and their coordinates would be shifted into the nearest TileArea position:

┌────────────────────┐
│    ┌────────┐      │
│    │X       │      │
│    │        │      │
│    └────────┘      │
└────────────────────┘
This is used for complex locations that use multiple tile areas to match a drawn map with a different layout. This can be omitted in most cases.

CustomFields	The custom fields for this entry.
MapNeighborIdAliases	(Optional) A set of aliases that can be used in tooltip fields like LeftNeighbor instead of the specific values they represent. Aliases can't be recursive.
For example, this lets you use Beach/FishShop in neighbor fields instead of specifying the specific tooltip IDs each time:

"MapNeighborIdAliases": {
    "Beach/FishShop": "Beach/FishShop_DefaultHours, Beach/FishShop_ExtendedHours"
}
Example

This Content Patcher content pack adds a new world map for Ginger Island. If the player unlocked the beach resort, it applies the beach resort texture.

{
    "Format": "2.5.0",
    "Changes": [
        // add world map edits
        {
            "Action": "EditData",
            "Target": "Data/WorldMap",
            "Entries": {
                "GingerIsland": {
                    "BaseTexture": [
                        {
                            "Id": "Default",
                            "Texture": "{{InternalAssetKey: assets/ginger-island.png}}"
                        }
                    ],
                    "MapAreas": [
                        // the Island South (dock) area
                        {
                            // basic info for the area within the map
                            "Id": "IslandSouth",
                            "PixelArea": { "X": 105, "Y": 105, "Width": 231, "Height": 240 },
                            "ScrollText": "Dock", // example only, should usually be translated

                            // a tooltip shown when hovering over the area on the map
                            "Tooltips": [
                                {
                                    "Id": "Dock",
                                    "Text": "Dock" // example only, should usually be translated
                                }
                            ],

                            // if the resort is unlocked, overlay a custom texture on top of the default Ginger Island map
                            "Textures": [
                                {
                                    "Id": "Resort",
                                    "Texture": "{{InternalAssetKey: assets/resort.png}}",
                                    "Condition": "PLAYER_HAS_FLAG Any Island_Resort"
                                }
                            ],

                            // the in-game locations that are part of this world map area
                            "WorldPositions": [
                                {
                                    "LocationName": "IslandSouth"
                                }
                            ]
                        }
                    ]
                }
            }
        }
    ]
}
Real-time positioning

The world map generally shows players' positions in real-time. There are three main approaches to do this for a custom location.

Automatic positioning (recommended)

If the drawn map area closely matches the in-game location, the game can determine positions automatically based on the PixelArea and LocationName fields in Data/WorldMap. For example, a player in the exact center of the in-game location will be drawn in the center of the drawn map area.

To do that:

Take a screenshot of the full in-game location.
Open the screenshot in an image editor like Paint.NET or GIMP.
Crop as needed, then rescale it to the size you want on the world map. Make sure you use 'nearest neighbor' as the scale algorithm.
Redraw parts if needed to clean it up.
That's it! If you use that as the map area's texture in Data/WorldMap, the game will be able to determine positions automatically. You can omit the WorldPositions field with this approach.

Manual positioning

If the in-game layout doesn't match the drawn world map, you can use the WorldPositions field in Data/WorldMap to manually align positions between them. This can be tricky; usually automatic positioning is recommended instead.

For example, the mountain's map area was very stylized before Stardew Valley 1.6 (the mine and adventure guild were right next to each other, there were no islands, there was no water south of the guild, etc):


The pre-1.6 mountain's in-game location (top) and world map area (bottom).
With manual positioning, you add any number of world positions with a TileArea (the tile coordinates where the player is standing in the actual location) and MapPixelArea (where that area is on the map). When the player is within the TileArea, they'll be mapped to the relative position within the matching MapPixelArea. For example, if they're in the exact center of the TileArea, they'll be drawn in the center of the MapPixelArea.

For example, you could divide the pre-1.6 mountain into multiple areas like this (see the data format for info on each field):

"WorldPositions": [
    {
        "Id": "Quarry",
        "LocationName": "Mountain",
        "TileArea": { "X": 95, "Y": 11, "Width": 36, "Height": 24 },
        "ExtendedTileArea": { "X": 95, "Y": 0, "Width": 255, "Height": 255 },
        "MapPixelArea": { "X": 236, "Y": 29, "Width": 28, "Height": 19 }
    },
    {
        "Id": "Lake_Guild",
        "LocationName": "Mountain",
        "TileArea": { "X": 73, "Y": 5, "Width": 22, "Height": 30 },
        "ExtendedTileArea": { "X": 73, "Y": 0, "Width": 22, "Height": 255 },
        "MapPixelArea": { "X": 227, "Y": 29, "Width": 9, "Height": 19 }
    },
    {
        "Id": "Lake_BetweenGuildAndMine",
        "LocationName": "Mountain",
        "TileArea": { "X": 57, "Y": 5, "Width": 16, "Height": 32 },
        "ExtendedTileArea": { "X": 57, "Y": 0, "Width": 16, "Height": 255 },
        "MapPixelArea": { "X": 224, "Y": 29, "Width": 3, "Height": 19 }
    },
    {
        "Id": "Lake_Mine",
        "LocationName": "Mountain",
        "TileArea": { "X": 52, "Y": 5, "Width": 5, "Height": 30 },
        "ExtendedTileArea": { "X": 52, "Y": 0, "Width": 5, "Height": 255 },
        "MapPixelArea": { "X": 220, "Y": 29, "Width": 4, "Height": 19 }
    },
    {
        "Id": "Lake_MineBridge",
        "LocationName": "Mountain",
        "TileArea": { "X": 44, "Y": 5, "Width": 8, "Height": 30 },
        "ExtendedTileArea": { "X": 44, "Y": 0, "Width": 8, "Height": 255 },
        "MapPixelArea": { "X": 210, "Y": 29, "Width": 10, "Height": 19 }
    },
    {
        "Id": "West",
        "LocationName": "Mountain",
        "TileArea": { "X": 0, "Y": 5, "Width": 44, "Height": 30 },
        "ExtendedTileArea": { "X": 0, "Y": 0, "Width": 44, "Height": 255 },
        "MapPixelArea": { "X": 175, "Y": 29, "Width": 35, "Height": 19 }
    },
    {
        "Id": "Default",
        "LocationName": "Mountain"
    }
]
Here's a visual representation of those areas:


The pre-1.6 mountain's in-game location with highlighted TileArea positions (top) and world map with highlighted MapPixelArea positions (bottom).
Note how the area between the mine and adventurer's guild is wide in the location, but narrow on the drawn world map. When the player is walking across that part of the location, they'll be shown walking slowly across the equivalent location on the drawn map.

If the player is outside a TileArea but within the ExtendedTileArea (if set), their position is snapped to the nearest position within the TileArea. For example, notice how the bottom of the location south of the carpenter shop isn't part of the red area. It is part of that area's ExtendedTileArea though, so a player there will be snapped to the bottom of the red area on the world map.

Fixed positions

For very complex locations, real-time positions on the world map may not be possible (e.g. because the drawn world map is very stylized). In that case you can set a fixed position (or multiple fixed positions) on the world map.

For example, this draws the player marker at one of five world map positions depending where they are in town. The TileArea indicates the tile coordinates where the player is standing in the actual town, and MapPixelArea is where to draw them on the map. Note that the latter is always 1x1 pixel in the code below, which means that anywhere within the TileArea will be placed on that specific pixel on the world map. The last entry has no TileArea, which means it applies to all positions that didn't match a previous entry.

"WorldPositions": [
    {
        "Id": "East_NearJojaMart",
        "LocationName": "Town",
        "TileArea": { "X": 85, "Y": 0, "Width": 255, "Height": 68 },
        "MapPixelArea": { "X": 225, "Y": 81, "Width": 1, "Height": 1 }
    },
    {
        "Id": "East_NearMuseum",
        "LocationName": "Town",
        "TileArea": { "X": 81, "Y": 68, "Width": 255, "Height": 255 },
        "MapPixelArea": { "X": 220, "Y": 108, "Width": 1, "Height": 1 }
    },
    {
        "Id": "West_North",
        "LocationName": "Town",
        "TileArea": { "X": 0, "Y": 0, "Width": 85, "Height": 43 },
        "MapPixelArea": { "X": 178, "Y": 64, "Width": 1, "Height": 1 }
    },
    {
        "Id": "West_Center",
        "LocationName": "Town",
        "TileArea": { "X": 0, "Y": 43, "Width": 85, "Height": 33 },
        "MapPixelArea": { "X": 175, "Y": 88, "Width": 1, "Height": 1 }
    },
    {
        "Id": "West_South",
        "LocationName": "Town",
        "MapPixelArea": { "X": 182, "Y": 109, "Width": 0, "Height": 0 }
    }
]
Interacting with the world map in C#

SMAPI mods (written in C#) can use the game's StardewValley.WorldMaps.WorldMapManager class to interact with the world map.

For example, you can get the pixel position on the world map which matches an in-game tile coordinate (if the location appears in Data/WorldMap):

MapAreaPosition mapAreaPosition = WorldMapManager.GetPositionData(location, tile);
if (mapAreaPosition != null)
    return mapAreaPosition.GetMapPixelPosition(location, tile);
Debug view

You can run debug worldMapLines in the SMAPI console window to enable the world map's debug view. This will outline map areas (black), map area positions (blue), and tooltips (green):


The world map with the debug view enabled.
You can optionally specify which types to highlight, like debug worldMapLines areas positions tooltips.

Default starting town locations are:
1 River Road
1 Willow Lane
2 Willow Lane
Blacksmith
Community Center
Dog Pen
Graveyard
Harvey's Clinic
Ice Cream Stand
Joja Warehouse
JojaMart
Mayor's Manor
Movie Theater
Museum
Pierre's General Store
The Sewers
The Stardrop Saloon
Trailer