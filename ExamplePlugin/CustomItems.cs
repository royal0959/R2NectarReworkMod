﻿using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace NectarRework
{
    internal class CustomItems
    {

        public static ItemDef ItemOrganicAllyBuff;
        public static ItemDef ItemWispName;


        public static void Init()
        {
            //CreateGrowthNectarV2();
            CreateOrganicAllyBuff();
            CreateWispNameItem();

            OverrideDefaultBehavior();

            AddLanguageTokens();
            On.RoR2.CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;
            RoR2Application.onLoad += OnLoad;
        }

        private static void OnLoad()
        {
            DLC2Content.Items.BoostAllStats.tags =
                           [
                    ItemTag.Damage,
                    ItemTag.AIBlacklist,
                    ItemTag.CannotCopy,
                    ItemTag.BrotherBlacklist
                ];
        }

        private static void OverrideDefaultBehavior()
        {
            IL.RoR2.CharacterBody.RecalculateStats += (il) =>
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(
                    x => x.MatchLdloc(53),
                    x => x.MatchLdcI4(0)
                    ))
                {
                    Log.Info("[GrowthNectarRework]: IL Hook succeeded");
                    c.Index += 1;
                    Log.Info($"[GrowthNectarRework]: OpCode before: {c.Next.OpCode}");
                    c.Remove();
                    c.Emit(OpCodes.Ldc_I4, 999999);// make the check require a bajillion nectars to pass 
                    Log.Info($"[GrowthNectarRework]: operand after: {c.Next.Operand}");
                    Log.Info($"[GrowthNectarRework]: OpCode after: {c.Next.Operand}");


                }
                else
                {
                    Log.Info("[GrowthNectarRework]: IL Hook failed");
                }
            };
        }

        private static void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            if (NetworkServer.active)
            {
                self.AddItemBehavior<OrganicAllyBuff>(self.inventory.GetItemCount(ItemOrganicAllyBuff));

                // override behavior
                self.AddItemBehavior<GrowthNectarV2>(self.inventory.GetItemCount(DLC2Content.Items.BoostAllStats));
            }
            orig(self);
        }

        private static void CreateOrganicAllyBuff()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ItemOrganicAllyBuff = new ItemDef
            {
                name = "OrganicAllyBuff",
                //tier = ItemTier.NoTier,
                //_itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion(),
                _itemTierDef = null,

                deprecatedTier = ItemTier.NoTier,
                pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion(),
                pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion(),
                nameToken = "ITEM_ORGANICALLYBUFF_NAME",
                pickupToken = "ITEM_ORGANICALLYBUFF_PICKUP",
                descriptionToken = "ITEM_ORGANICALLYBUFF_DESC",
                loreToken = "ITEM_ORGANICALLYBUFF_LORE",
                tags = new[]
                {
                    ItemTag.WorldUnique,
                    ItemTag.CannotCopy,
                    ItemTag.CannotDuplicate,
                    ItemTag.BrotherBlacklist,
                    ItemTag.CannotSteal,
                },

                canRemove = false,
                hidden = true
            };
#pragma warning restore CS0618 // Type or member is obsolete

            var displayRules = new ItemDisplayRuleDict(null);

            var itemIndex = new CustomItem(ItemOrganicAllyBuff, displayRules);
            ItemAPI.Add(itemIndex);
        }

        private static void CreateWispNameItem()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ItemWispName = new ItemDef
            {
                name = "GuardianWispName",
                //tier = ItemTier.NoTier,
                //_itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion(),
                _itemTierDef = null,

                deprecatedTier = ItemTier.NoTier,
                pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion(),
                pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion(),
                nameToken = "ITEM_GUARDIANWISP_NAME",
                pickupToken = "ITEM_GUARDIANWISP_PICKUP",
                descriptionToken = "ITEM_GUARDIANWISP_DESC",
                loreToken = "ITEM_GUARDIANWISP_LORE",
                tags = new[]
                {
                    ItemTag.WorldUnique,
                    ItemTag.CannotCopy,
                    ItemTag.CannotDuplicate,
                    ItemTag.BrotherBlacklist,
                    ItemTag.CannotSteal,
                },

                canRemove = false,
                hidden = true
            };
#pragma warning restore CS0618 // Type or member is obsolete

            var displayRules = new ItemDisplayRuleDict(null);

            var itemIndex = new CustomItem(ItemWispName, displayRules);
            ItemAPI.Add(itemIndex);
        }


        //private static void CreateGrowthNectarV2()
        //{
        //    GrowthNectarV2 = new ItemDef
        //    {
        //        name = "GrowthNectarV2",
        //        tier = ItemTier.Tier3,
        //        _itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion(),
        //        pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Items/BoostAllStats/DisplayGrowthNectar.prefab").WaitForCompletion(),
        //        pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC2/Items/BoostAllStats/texGrowthNectarIcon.png").WaitForCompletion(),
        //        nameToken = "ITEM_NEWBOOSTALLSTATS_NAME",
        //        pickupToken = "ITEM_NEWBOOSTALLSTATS_PICKUP",
        //        descriptionToken = "ITEM_NEWBOOSTALLSTATS_DESC",
        //        loreToken = "ITEM_NEWBOOSTALLSTATS_LORE",
        //        tags = new[]
        //        {
        //            ItemTag.Damage,
        //            ItemTag.AIBlacklist,
        //            ItemTag.BrotherBlacklist
        //        }
        //    };

        //    var displayRules = new ItemDisplayRuleDict(null);

        //    var itemIndex = new CustomItem(GrowthNectarV2, displayRules);
        //    ItemAPI.Add(itemIndex);
        //}

        private static void AddLanguageTokens()
        {
            LanguageAPI.Add("ITEM_BOOSTALLSTATS_PICKUP", "Summon a <style=cIsUtility>Guardian Wisp</style>. All <style=cIsUtility>organic</style> allies are <style=cIsDamage>stronger</style> and <style=cIsDamage>Elite</style>.");
            LanguageAPI.Add("ITEM_BOOSTALLSTATS_DESC",
                "Gain an allied <style=cIsUtility>Guardian Wisp</style> that respawns every 30 seconds. All <style=cIsUtility>ORGANIC</style> allies will gain <style=cIsDamage>+200%</style> <style=cStack>(+200% per stack)</style> damage and <style=cIsUtility>+150%</style> <style=cStack>(+150% per stack)</style> health and a random <style=cIsDamage>Elite</style> status.\r\n");
        }
    }
}
