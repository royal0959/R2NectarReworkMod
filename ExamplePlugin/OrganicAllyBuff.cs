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

namespace NectarRework
{
    public class OrganicAllyBuff : CharacterBody.ItemBehavior
    {
        private const float HEALTH_MULTIPLIER = 1.5f; // 150%
        private const int DAMAGE_MULTIPLIER = 2; // 150%

        private void Awake()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;

            base.enabled = false;
        }

        //private void OnEnable()
        //{
        //    // Any initialisation logic, null check for `this.body` as necessary
        //    // `this.stack` is still unassigned at this point so use `this.body.inventory`
        //}

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

        //private void OnDisable()
        //{

        //}

        //private void FixedUpdate()
        //{

        //}
    }
}
