using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ExamplePlugin
{
    internal class Items
    {
        internal static ItemDef GrowthNectarV2;

        public static void Init()
        {
            CreateGrowthNectarV2();

            AddLanguageTokens();
            On.RoR2.CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;
        }

        private static void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            if (NetworkServer.active)
            {
                self.AddItemBehavior<GrowthNectarV2>(self.inventory.GetItemCount(GrowthNectarV2));
            }
            orig(self);
        }

        private static void CreateGrowthNectarV2()
        {
            GrowthNectarV2 = new ItemDef
            {
                name = "GrowthNectarV2",
                tier = ItemTier.Tier3,
                pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Items/BoostAllStats/DisplayGrowthNectar.prefab").WaitForCompletion(),
                pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC2/Items/BoostAllStats/texGrowthNectarIcon.png").WaitForCompletion(),
                nameToken = "ITEM_NEWBOOSTALLSTATS_NAME",
                pickupToken = "ITEM_NEWBOOSTALLSTATS_PICKUP",
                descriptionToken = "ITEM_NEWBOOSTALLSTATS_DESC",
                loreToken = "ITEM_NEWBOOSTALLSTATS_LORE",
                tags = new[]
                {
                    ItemTag.Damage,
                    ItemTag.AIBlacklist,
                    ItemTag.BrotherBlacklist
                }
            };

            //#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            //GrowthNectarV2._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

            var displayRules = new ItemDisplayRuleDict(null);

            var itemIndex = new CustomItem(GrowthNectarV2, displayRules);
            ItemAPI.Add(itemIndex);
        }

        private static void AddLanguageTokens()
        {
            LanguageAPI.Add("ITEM_NEWBOOSTALLSTATS_NAME", "Growth Nectar v2");
            LanguageAPI.Add("ITEM_NEWBOOSTALLSTATS_PICKUP", "Summon a <style=cIsUtility>Greater Wisp</style>. All <style=cIsUtility>organic</style> allies are <style=cIsDamage>stronger</style> and <style=cIsDamage>Elite</style>.");
            LanguageAPI.Add("ITEM_NEWBOOSTALLSTATS_DESC",
                "Gain an allied <style=cIsUtility>Greater Wisp</style> that respawns every 30 seconds. All <style=cIsUtility>NON-MECHANICAL</style> allies will gain  <style=cIsDamage>+200%</style> <style=cStack>(+200% per stack)</style> damage and <style=cIsUtility>+150%</style> <style=cStack>(+150% per stack)</style> health and a random <style=cIsDamage>Elite</style> status.\r\n");
            LanguageAPI.Add("ITEM_NEWBOOSTALLSTATS_LORE",
                "Hello everybody my name is markiplier welcome to five nights at freddy's.");
        }
    }
}
