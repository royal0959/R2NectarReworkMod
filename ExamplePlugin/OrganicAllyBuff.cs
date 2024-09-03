using RoR2;
using RoR2.Items;
using BepInEx;
using R2API;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Random = System.Random;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Linq;
using MonoMod.Cil;

namespace NectarRework
{
    public class OrganicAllyBuff : CharacterBody.ItemBehavior
    {
        private const float HEALTH_MULTIPLIER = 1.5f; // 150%
        private const int DAMAGE_MULTIPLIER = 2; // 150%

        private void Awake()
        {
            GameModeCatalog.availability.CallWhenAvailable(new Action(PostLoad));

            base.enabled = false;
        }

        public void PostLoad()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
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

            args.healthMultAdd += HEALTH_MULTIPLIER * stack;
            args.damageMultAdd += DAMAGE_MULTIPLIER * stack;
        }
    }
}
