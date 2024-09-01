using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ExamplePlugin
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "AuthorName";
        public const string PluginName = "ExamplePlugin";
        public const string PluginVersion = "1.0.0";


        public void Awake()
        {
            Log.Init(Logger);
            Items.Init();
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
               
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(Items.GrowthNectarV2.itemIndex), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.BeetleGland.itemIndex), transform.position, transform.forward * 20f);

            }
        }
    }
}
