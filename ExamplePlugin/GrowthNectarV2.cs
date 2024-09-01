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

namespace ExamplePlugin
{
    public class GrowthNectarV2 : CharacterBody.ItemBehavior
    {
        // copied from DroneWeaponsBehavior
        private const float display2Chance = 0.1f;

        //private readonly EliteDef[] possibleEliteDefsArray = {
        //        RoR2Content.Elites.Lightning,
        //        RoR2Content.Elites.Ice,
        //        RoR2Content.Elites.Fire,
        //        DLC1Content.Elites.Earth
        //};

        // hardcode garbage because I don't know how to get BuffDef from EliteDef
        private readonly string[] eliteBuffNames = {
            "bdEliteLightning",
            "bdEliteIce",
            "bdEliteFire",
            "bdEliteEarth",
        };

        private List<BuffIndex> possibleEliteBuffsList = new List<BuffIndex>();
        private BuffIndex[] possibleEliteBuffsArray;

        private int previousStack;

        private CharacterSpawnCard droneSpawnCard;

        private Xoroshiro128Plus rng;

        private DirectorPlacementRule placementRule;

        private const float minSpawnDist = 3f;

        private const float maxSpawnDist = 40f;

        private const float spawnRetryDelay = 1f;

        private bool hasSpawnedDrone;

        private float spawnDelay;

        private void Awake()
        {
            for (int k = 0; k < BuffCatalog.eliteBuffIndices.Length; k++)
            {
                BuffIndex buffIndex = BuffCatalog.eliteBuffIndices[k];

                string buffName = BuffCatalog.GetBuffDef(buffIndex).name;

                if (!eliteBuffNames.Contains(buffName))
                {
                    continue;
                }

                Log.Info($"[GrowthNectarRework] added to pool {BuffCatalog.GetBuffDef(buffIndex).name}");
                possibleEliteBuffsList.Add(buffIndex);
            }

            possibleEliteBuffsArray = possibleEliteBuffsList.ToArray();

            //for (int k = 0; k < possibleEliteBuffsArray.Length; k++)
            //{
            //    Log.Info($"Buff Index: {k}; BuffDef name: {BuffCatalog.GetBuffDef(possibleEliteBuffsArray[k]).name}");
            //}

            base.enabled = false;
        }

        private void OnEnable()
        {
            // Any initialisation logic, null check for `this.body` as necessary
            // `this.stack` is still unassigned at this point so use `this.body.inventory`

            //Random random = new Random();

            //BuffIndex[] elitesArray = BuffCatalog.eliteBuffIndices;
            //BuffIndex chosenEliteIndex = elitesArray[random.Next(0, elitesArray.Length)];
            //this.body.AddBuff(chosenEliteIndex);

            ulong seed = Run.instance.seed ^ (ulong)Run.instance.stageClearCount;
            rng = new Xoroshiro128Plus(seed);
            droneSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/GreaterWisp/cscGreaterWisp.asset").WaitForCompletion();
            //droneSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/DroneCommander/cscDroneCommander.asset").WaitForCompletion();
            placementRule = new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                minDistance = 3f,
                maxDistance = 40f,
                spawnOnTarget = base.transform
            };
            UpdateAllMinions(stack);
            MasterSummon.onServerMasterSummonGlobal += OnServerMasterSummonGlobal;
        }
        private void OnDisable()
        {
            MasterSummon.onServerMasterSummonGlobal -= OnServerMasterSummonGlobal;
            UpdateAllMinions(0);
        }

        private void FixedUpdate()
        {
            spawnDelay -= Time.fixedDeltaTime;
            if (!hasSpawnedDrone && (bool)body && spawnDelay <= 0f)
            {
                TrySpawnDrone();
            }
        }

        private void TrySpawnDrone()
        {
            if (!body.master.IsDeployableLimited(DeployableSlot.DroneWeaponsDrone))
            {
                spawnDelay = 1f;
                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(droneSpawnCard, placementRule, rng);
                directorSpawnRequest.summonerBodyObject = base.gameObject;
                directorSpawnRequest.onSpawnedServer = OnSummonedSpawned;
                DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
            }
        }

        private void OnSummonedSpawned(SpawnCard.SpawnResult spawnResult)
        {
            hasSpawnedDrone = true;
            GameObject spawnedInstance = spawnResult.spawnedInstance;
            if (!spawnedInstance)
            {
                return;
            }

            CharacterMaster component = spawnedInstance.GetComponent<CharacterMaster>();
            if ((bool)component)
            {
                Deployable component2 = component.GetComponent<Deployable>();
                if ((bool)component2)
                {
                    body.master.AddDeployable(component2, DeployableSlot.DroneWeaponsDrone);
                }
            }
        }

        private void OnServerMasterSummonGlobal(MasterSummon.MasterSummonReport summonReport)
        {
            if (!body || !body.master || !(body.master == summonReport.leaderMasterInstance))
            {
                return;
            }
            CharacterMaster summonMasterInstance = summonReport.summonMasterInstance;
            if ((bool)summonMasterInstance)
            {
                CharacterBody characterBody = summonMasterInstance.GetBody();
                if ((bool)characterBody)
                {
                    UpdateMinionInventory(characterBody, summonMasterInstance.inventory, stack);
                }
            }
        }
        private void UpdateAllMinions(int newStack)
        {
            if (!body || !(body?.master))
            {
                return;
            }
            MinionOwnership.MinionGroup minionGroup = MinionOwnership.MinionGroup.FindGroup(body.master.netId);
            if (minionGroup == null)
            {
                return;
            }
            MinionOwnership[] members = minionGroup.members;
            foreach (MinionOwnership minionOwnership in members)
            {
                if (!minionOwnership)
                {
                    continue;
                }
                CharacterMaster component = minionOwnership.GetComponent<CharacterMaster>();
                if ((bool)component && (bool)component.inventory)
                {
                    CharacterBody characterBody = component.GetBody();
                    if ((bool)characterBody)
                    {
                        UpdateMinionInventory(characterBody, component.inventory, newStack);
                    }
                }
            }
            previousStack = newStack;
        }

        private void ResetMinionInventory(Inventory minionInventory)
        {
            // TODO: replace with proper item
            minionInventory.ResetItem(DLC1Content.Items.DroneWeaponsBoost);
        }

        private void UpdateMinionInventory(CharacterBody minionBody, Inventory minionInventory, int newStack)
        {
            if (!(bool)minionInventory)
            {
                ResetMinionInventory(minionInventory);
                return;
            }

            if (newStack <= 0)
            {
                ResetMinionInventory(minionInventory);
                return;
            }

            CharacterBody.BodyFlags bodyFlags = minionBody.bodyFlags;

            // non-mechanical allies only
            if ((bodyFlags & CharacterBody.BodyFlags.Mechanical) != 0)
            {
                ResetMinionInventory(minionInventory);
                return;
            }

            // TODO: replace with proper item
            int itemCount = minionInventory.GetItemCount(DLC1Content.Items.DroneWeaponsBoost);
            if (itemCount < stack)
            {
                minionInventory.GiveItem(DLC1Content.Items.DroneWeaponsBoost, stack - itemCount);
            }
            else if (itemCount > stack)
            {
                minionInventory.RemoveItem(DLC1Content.Items.DroneWeaponsBoost, itemCount - stack);
            }

            bool isDevotionSpawn = (bodyFlags & CharacterBody.BodyFlags.Devotion) != 0;


            if (!isDevotionSpawn && !minionBody.isElite)
            {
                Random random = new Random();
                BuffIndex chosenEliteIndex = possibleEliteBuffsArray[random.Next(0, possibleEliteBuffsArray.Length)];

                //Log.Info($"Chosen Buff Index: {chosenEliteIndex}; BuffDef name: {BuffCatalog.GetBuffDef(chosenEliteIndex).name}");

                minionBody.AddBuff(chosenEliteIndex);
            }
        }
    }
}
