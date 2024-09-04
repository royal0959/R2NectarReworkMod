using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace NectarRework
{
    public class Hooks
    {
        private const float EXTR_HEALTH_MULTIPLIER = 1.5f; // 150%
        private const float EXTR_DAMAGE_MULTIPLIER = 2f; // 200%

        internal static void Init()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
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
    }
}
