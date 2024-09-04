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

        private void Awake()
        {
            base.enabled = false;
        }
    }
}
