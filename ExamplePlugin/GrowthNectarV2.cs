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
using UnityEngine.Events;
using System.ComponentModel;

namespace NectarRework
{
    public class GrowthNectarV2 : CharacterBody.ItemBehavior
    {
        //private readonly EliteDef[] possibleEliteDefsArray = {
        //        RoR2Content.Elites.Lightning,
        //        RoR2Content.Elites.Ice,
        //        RoR2Content.Elites.Fire,
        //        DLC1Content.Elites.Earth
        //};

        // hardcode garbage because I don't know how to get BuffDef from EliteDef
        //private readonly string[] ELITE_BUFF_NAMES = {
        //    "bdEliteLightning",
        //    "bdEliteIce",
        //    "bdEliteFire",
        //    "bdEliteEarth",
        //};

        private readonly EquipmentDef[] POSSIBLE_ELITE_EQUIPMENTS =
        {
            RoR2Content.Elites.Lightning.eliteEquipmentDef,
             RoR2Content.Elites.Ice.eliteEquipmentDef,
             RoR2Content.Elites.Fire.eliteEquipmentDef,
             DLC1Content.Elites.Earth.eliteEquipmentDef
        };

        //private const int DAMAGE_ITEM_STACK_COUNT = 20; // 200%
        //private const int HEALTH_ITEM_STACK_COUNT = 15; // 150%

        //private List<BuffIndex> possibleEliteBuffsList = new List<BuffIndex>();
        //private BuffIndex[] possibleEliteBuffsArray;

        // copied from DroneWeaponsBehavior
        private int previousStack;
        private CharacterSpawnCard droneSpawnCard;
        private Xoroshiro128Plus rng;
        private DirectorPlacementRule placementRule;

        private CharacterBody summonedWispBody;

        private const float spawnRetryDelay = 1f;
        private const float spawnCooldown = 30f;

        private float spawnTimer = 0f;

        DeployableSlot wispDeployable;

        //private bool hasSpawnedDrone;
        //private float spawnDelay;

        public int GetWishDeployableSlotLimit(CharacterMaster self, int deployableCountMultiplier)
        {
            return 1 * deployableCountMultiplier;
        }

        private void Awake()
        {
            GameModeCatalog.availability.CallWhenAvailable(new Action(PostLoad));

            //for (int k = 0; k < BuffCatalog.eliteBuffIndices.Length; k++)
            //{
            //    BuffIndex buffIndex = BuffCatalog.eliteBuffIndices[k];

            //    string buffName = BuffCatalog.GetBuffDef(buffIndex).name;

            //    if (!ELITE_BUFF_NAMES.Contains(buffName))
            //    {
            //        continue;
            //    }

            //    Log.Info($"[GrowthNectarRework] added to pool {BuffCatalog.GetBuffDef(buffIndex).name}");
            //    possibleEliteBuffsList.Add(buffIndex);
            //}

            //possibleEliteBuffsArray = possibleEliteBuffsList.ToArray();

            //for (int k = 0; k < possibleEliteBuffsArray.Length; k++)
            //{
            //    Log.Info($"Buff Index: {k}; BuffDef name: {BuffCatalog.GetBuffDef(possibleEliteBuffsArray[k]).name}");
            //}

            base.enabled = false;
        }

        public void PostLoad()
        {
            wispDeployable = DeployableAPI.RegisterDeployableSlot(GetWishDeployableSlotLimit);
            On.RoR2.Util.GetBestBodyName += Util_GetBestBodyName;
        }

        // rename wisp
        private string Util_GetBestBodyName(On.RoR2.Util.orig_GetBestBodyName orig, GameObject bodyObject)
        {
            string text = orig(bodyObject);

            CharacterBody characterBody = null;

            if ((bool)bodyObject)
            {
                characterBody = bodyObject.GetComponent<CharacterBody>();
            }

            //Log.Info($"characterBody {characterBody} Wisp body {summonedWispBody} Equal: {characterBody == summonedWispBody}");

            if (characterBody != null && characterBody == summonedWispBody)
            {
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

        private void OnEnable()
        {
            // Any initialisation logic, null check for `this.body` as necessary
            // `this.stack` is still unassigned at this point so use `this.body.inventory`

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

            UpdateAllMinions(this.body.inventory.GetItemCount(DLC2Content.Items.BoostAllStats));
            MasterSummon.onServerMasterSummonGlobal += OnServerMasterSummonGlobal;
        }

        private void OnDisable()
        {
            MasterSummon.onServerMasterSummonGlobal -= OnServerMasterSummonGlobal;
            UpdateAllMinions(0);
        }

        private void FixedUpdate()
        {
            if (previousStack != stack)
            {
                UpdateAllMinions(stack);
            }

            //spawnDelay -= Time.fixedDeltaTime;
            //if (!hasSpawnedDrone && (bool)body && spawnDelay <= 0f)
            //{
            //    TrySpawnDrone();
            //}

            CharacterMaster bodyMaster = base.body.master;
            if (!bodyMaster)
            {
                return;
            }

            int deployableCount = bodyMaster.GetDeployableCount(wispDeployable);

            if (deployableCount >= 1)
            {
                return;
            }

            spawnTimer -= Time.fixedDeltaTime;
            if (spawnTimer <= 0f)
            {
                TrySpawnDrone();
            }
        }

        private void TrySpawnDrone()
        {
            if (!body.master.IsDeployableLimited(wispDeployable))
            {
                spawnTimer = spawnRetryDelay;
                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(droneSpawnCard, placementRule, rng);
                directorSpawnRequest.summonerBodyObject = base.gameObject;
                directorSpawnRequest.onSpawnedServer = OnSummonedSpawned;
                DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
            }
        }

        private void OnSummonedSpawned(SpawnCard.SpawnResult spawnResult)
        {
            spawnTimer = spawnCooldown;

            //hasSpawnedDrone = true;

            GameObject spawnedInstance = spawnResult.spawnedInstance;
            if (!spawnedInstance)
            {
                return;
            }

            CharacterMaster component = spawnedInstance.GetComponent<CharacterMaster>();
            if ((bool)component)
            {
                summonedWispBody = component.GetBody();

                component.inventory.GiveItem(RoR2Content.Items.Hoof, 3);
                component.inventory.GiveItem(RoR2Content.Items.Syringe, 2);

                Deployable component2 = component.GetComponent<Deployable>();

                if (!(bool)component2)
                {
                    component2 = component.gameObject.AddComponent<Deployable>();
                    component2.onUndeploy = new UnityEvent();
                    component2.onUndeploy.AddListener(component.TrueKill);
                }

                if ((bool)component2)
                {
                    body.master.AddDeployable(component2, wispDeployable);
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
            //minionInventory.ResetItem(RoR2Content.Items.BoostDamage);
            //minionInventory.ResetItem(RoR2Content.Items.BoostHp);
            minionInventory.ResetItem(CustomItems.ItemOrganicAllyBuff);
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

            int itemCount = minionInventory.GetItemCount(CustomItems.ItemOrganicAllyBuff);

            if (itemCount < stack)
            {
                int count = (stack - itemCount);
                //int damageStackCount = baseCount * DAMAGE_ITEM_STACK_COUNT;
                //int healthStackCount = baseCount * HEALTH_ITEM_STACK_COUNT;

                minionInventory.GiveItem(CustomItems.ItemOrganicAllyBuff, count);
                Log.Info($"Buff stack: {minionInventory.GetItemCount(CustomItems.ItemOrganicAllyBuff)}");

                //minionInventory.GiveItem(RoR2Content.Items.BoostDamage, damageStackCount);
                //minionInventory.GiveItem(RoR2Content.Items.BoostHp, healthStackCount);

                //Log.Info($"Damage items count: {minionInventory.GetItemCount(RoR2Content.Items.BoostDamage)}; Health items count: {minionInventory.GetItemCount(RoR2Content.Items.BoostHp)}");
            }
            else if (itemCount > stack)
            {
                int count = (itemCount - stack);
                //int damageStackCount = baseCount * DAMAGE_ITEM_STACK_COUNT;
                //int healthStackCount = baseCount * HEALTH_ITEM_STACK_COUNT;

                minionInventory.GiveItem(CustomItems.ItemOrganicAllyBuff, count);
                Log.Info($"Buff stack: {minionInventory.GetItemCount(CustomItems.ItemOrganicAllyBuff)}");

                //minionInventory.RemoveItem(RoR2Content.Items.BoostDamage, damageStackCount);
                //minionInventory.GiveItem(RoR2Content.Items.BoostHp, healthStackCount);

                //Log.Info($"Damage items count: {minionInventory.GetItemCount(RoR2Content.Items.BoostDamage)}; Health items count: {minionInventory.GetItemCount(RoR2Content.Items.BoostHp)}");
            }

            bool isDevotionSpawn = (bodyFlags & CharacterBody.BodyFlags.Devotion) != 0;

            if (!isDevotionSpawn && !minionBody.isElite)
            {
                Random random = new Random();
                EquipmentDef chosenEliteEquipment = POSSIBLE_ELITE_EQUIPMENTS[random.Next(0, POSSIBLE_ELITE_EQUIPMENTS.Length)];

                //Log.Info($"Chosen elite equipment: {chosenEliteEquipment}; elite equipment index: {chosenEliteEquipment.equipmentIndex}");

                minionInventory.SetEquipmentIndex(chosenEliteEquipment.equipmentIndex);
                //minionBody.AddBuff(chosenEliteIndex);
            }
        }
    }
}
