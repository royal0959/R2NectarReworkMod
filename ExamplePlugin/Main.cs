using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ExamplePlugin
{
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    // It's a small plugin that adds a relatively simple item to the game,
    // and gives you that item whenever you press F2.

    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class Main : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "AuthorName";
        public const string PluginName = "ExamplePlugin";
        public const string PluginVersion = "1.0.0";

        // We need our item definition to persist through our functions, and therefore make it a class field.
        private static ItemDef myItemDef;

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            // First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            myItemDef.name = "GrowthNectarV2";
            myItemDef.nameToken = "ITEM_NEWBOOSTALLSTATS_NAME";
            myItemDef.pickupToken = "ITEM_NEWBOOSTALLSTATS_PICKUP";
            myItemDef.descriptionToken = "ITEM_NEWBOOSTALLSTATS_DESC";
            myItemDef.loreToken = "ITEM_NEWBOOSTALLSTATS_LORE";

            // The tier determines what rarity the item is:
            // Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            // and finally NoTier is generally used for helper items, like the tonic affliction
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            // Instead of loading the itemtierdef directly, you can also do this like below as a workaround
            // myItemDef.deprecatedTier = ItemTier.Tier2;

            // You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks.
            myItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC2/Items/BoostAllStats/texGrowthNectarIcon.png").WaitForCompletion();
            //myItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            myItemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();

            // Can remove determines
            // if a shrine of order,
            // or a printer can take this item,
            // generally true, except for NoTier items.
            myItemDef.canRemove = true;

            // Hidden means that there will be no pickup notification,
            // and it won't appear in the inventory at the top of the screen.
            // This is useful for certain noTier helper items, such as the DrizzlePlayerHelper.
            myItemDef.hidden = false;

            // You can add your own display rules here,
            // where the first argument passed are the default display rules:
            // the ones used when no specific display rules for a character are found.
            // For this example, we are omitting them,
            // as they are quite a pain to set up without tools like https://thunderstore.io/package/KingEnderBrine/ItemDisplayPlacementHelper/
            var displayRules = new ItemDisplayRuleDict(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            // But now we have defined an item, but it doesn't do anything yet. So we'll need to define that ourselves.
            //GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;

            AddLanguageTokens();

            On.RoR2.CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;
        }

        //private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        //{
        //    // If a character was killed by the world, we shouldn't do anything.
        //    if (!report.attacker || !report.attackerBody)
        //    {
        //        return;
        //    }

        //    var attackerCharacterBody = report.attackerBody;

        //    // We need an inventory to do check for our item
        //    if (attackerCharacterBody.inventory)
        //    {
        //        // Store the amount of our item we have
        //        var garbCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
        //        if (garbCount > 0 &&
        //            // Roll for our 50% chance.
        //            Util.CheckRoll(50, attackerCharacterBody.master))
        //        {
        //            // Since we passed all checks, we now give our attacker the cloaked buff.
        //            // Note how we are scaling the buff duration depending on the number of the custom item in our inventory.
        //            attackerCharacterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, 3 + garbCount);
        //        }
        //    }
        //}

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            if (NetworkServer.active)
            {
                // Setting the behavior stacks to 1 or 0 may be more appropriate
                // by checking if it exists in the inventory at all.
                self.AddItemBehavior<GrowthNectarV2>(self.inventory.GetItemCount(myItemDef));
            }
            orig(self);
        }

        private static void AddLanguageTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("ITEM_NEWBOOSTALLSTATS_NAME", "Growth Nectar v2");
            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, nuimbers are generally ommited.
            LanguageAPI.Add("ITEM_NEWBOOSTALLSTATS_PICKUP", "Summon a <style=cIsUtility>Greater Wisp</style>. All <style=cIsUtility>organic</style> allies are <style=cIsDamage>stronger</style> and <style=cIsDamage>Elite</style>.");
            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("ITEM_NEWBOOSTALLSTATS_DESC",
                "Gain an allied <style=cIsUtility>Greater Wisp</style> that respawns every 30 seconds. All <style=cIsUtility>NON-MECHANICAL</style> allies will gain  <style=cIsDamage>+200%</style> <style=cStack>(+200% per stack)</style> damage and <style=cIsUtility>+150%</style> <style=cStack>(+150% per stack)</style> health and a random <style=cIsDamage>Elite</style> status.\r\n");
            //LanguageAPI.Add("ITEM_NEWBOOSTALLSTATS_DESC",
            //"Grants <style=cDeath>RAMPAGE</style> on kill. \n<style=cDeath>RAMPAGE</style> : Specifics rewards for reaching kill streaks. \nIncreases <style=cIsUtility>movement speed</style> by <style=cIsUtility>1%</style> <style=cIsDamage>(+1% per item stack)</style> <style=cStack>(+1% every 20 Rampage Stacks)</style>. \nIncreases <style=cIsUtility>damage</style> by <style=cIsUtility>2%</style> <style=cIsDamage>(+2% per item stack)</style> <style=cStack>(+2% every 20 Rampage Stacks)</style>.");
            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("ITEM_NEWBOOSTALLSTATS_LORE",
                "Hello everybody my name is markiplier welcome to five nights at freddy's.");
        }

        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.

                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
