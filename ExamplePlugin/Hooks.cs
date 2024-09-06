using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NectarRework
{
    public class Hooks
    {
        private const float EXTR_HEALTH_MULTIPLIER = 1.5f; // 150%
        private const float EXTR_DAMAGE_MULTIPLIER = 2f; // 200%

        internal static void Init()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            On.RoR2.Util.GetBestBodyName += Util_GetBestBodyName;
        }

        public static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (!sender.inventory)
            {
                return;
            }

            int stack = sender.inventory.GetItemCount(CustomItems.ItemOrganicAllyBuff);

            if (stack <= 0)
            {
                return;
            }


            args.healthMultAdd += EXTR_HEALTH_MULTIPLIER * stack;
            args.damageMultAdd += EXTR_DAMAGE_MULTIPLIER * stack;

            //Log.Info($"healthMultAdd {args.healthMultAdd} damageMultAdd {args.damageMultAdd} added health: {EXTR_HEALTH_MULTIPLIER * stack}, added damage: {EXTR_DAMAGE_MULTIPLIER * stack}");
        }

        // rename wisp
        private static string Util_GetBestBodyName(On.RoR2.Util.orig_GetBestBodyName orig, GameObject bodyObject)
        {
            string text = orig(bodyObject);

            CharacterBody characterBody = null;

            if ((bool)bodyObject)
            {
                characterBody = bodyObject.GetComponent<CharacterBody>();
            }

            //Log.Info($"characterBody {characterBody} Wisp body {summonedWispBody} Equal: {characterBody == summonedWispBody}");

            if (characterBody != null)
            {
                int stack = characterBody.inventory.GetItemCount(CustomItems.ItemWispName);

                if (stack <= 0)
                {
                    return text;
                }

                string wispName = "Guardian Wisp";
                if (characterBody.isElite)
                {
                    BuffIndex[] eliteBuffIndices = BuffCatalog.eliteBuffIndices;
                    foreach (BuffIndex buffIndex in eliteBuffIndices)
                    {
                        if (characterBody.HasBuff(buffIndex))
                        {
                            wispName = Language.GetStringFormatted(BuffCatalog.GetBuffDef(buffIndex).eliteDef.modifierToken, wispName);
                        }
                    }
                }

                return wispName;
            }

            return text;
        }
    }
}
